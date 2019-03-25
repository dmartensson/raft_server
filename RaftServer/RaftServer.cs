using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RaftCommon;

namespace RaftServer
{
    // ReSharper disable once UnusedMember.Global
    public class RaftServer
    {
        private uint _currentTerm;
        private string _votedFor;
        private ulong _commitIndex;
        private ulong _lastApplied;
        private RaftState _state = RaftState.Offline;
        private readonly Random _rand = new Random();
        private int _heartbeat;

        private Dictionary<string, ulong> _nextIndex;
        private Dictionary<string, ulong> _matchIndex;

        private readonly HashSet<string> _peers;
        private string _me;
        private string _leader;

        private HashSet<string> _votesReceived;

        private readonly IRaftLog _log;
        private readonly IRaftTransport _transport;
        private readonly IRaftStorage _storage;
        private readonly IStateMachine _statemachine;
        private HashSet<string> _quorum;
        private readonly IRaftSerializer _raftSerializer;
        private readonly IRaftDiagnostic _raftDiagnostic;

        public RaftServer(IRaftLog log, IRaftTransport transport, IRaftStorage storage, IStateMachine statemachine, IRaftSerializer raftSerializer, IRaftDiagnostic raftDiagnostic = null)
        {
            _log = log;
            _transport = transport;
            _storage = storage;
            _statemachine = statemachine;
            _raftSerializer = raftSerializer;
            _peers = new HashSet<string>();
            _raftDiagnostic = raftDiagnostic ?? new DummyDiagnostics();
        }

        public async Task Run(CancellationToken ct)
        {
            var config = _raftSerializer.Deserialize<RaftConfig>(_storage.Read("config"));
            foreach (var peer in config.Peers)
                _peers.Add(peer);
            _me = config.Me ?? Guid.NewGuid().ToString();
            _heartbeat = config.Heartbeat ?? 50;
            _raftDiagnostic?.LoadedConfig(config);

            _statemachine.RegisterCommand("join", data => {
                //Ev. kontroller om vi ska acceptera, ex. lösenord eller liknande
                _peers.Add(_raftSerializer.Deserialize<string>(data));
                //Save config
                return _raftSerializer.Serialize(true);
            });
            _statemachine.RegisterCommand("leave", data => {
                //Ev. kontroller om vi ska acceptera
                _peers.Remove(_raftSerializer.Deserialize<string>(data));
                //Save config
                return _raftSerializer.Serialize(true);
            });
            _statemachine.RegisterCommand("peers", data => _raftSerializer.Serialize(_peers));


            _state = RaftState.Follower;
            while (!ct.IsCancellationRequested)
            {
                _votedFor = null;
                _votesReceived = null;
                _nextIndex = null;
                _matchIndex = null;
                _quorum = null;

                _raftDiagnostic?.EnterState(_state);
                switch (_state)
                {
                    case RaftState.Follower: await FollowerLoop(ct); break;
                    case RaftState.Candidate: await CandidateLoop(ct); break;
                    case RaftState.Leader: await LeaderLoop(ct); break;
                    case RaftState.Offline: return;
                    case RaftState.Snapshot: throw new NotImplementedException();
                    default:
                        throw new Exception("Unknown RaftState " + Enum.GetName(_state.GetType(), _state));
                }
            }
        }

        private async Task LeaderLoop(CancellationToken ct)
        {
            _statemachine.RegisterCommand("join", _ => null); //TODO Is this a good way
            _statemachine.RegisterCommand("leave", _ => null);
            long heartbeat = _heartbeat;
            var watch = new Stopwatch();
            watch.Start();
            var lastCheck = watch.ElapsedMilliseconds;
            var lastIndex = _log.LastEntry().index + 1;
            _nextIndex = _peers.ToDictionary(p => p, p => lastIndex);
            _matchIndex = _peers.ToDictionary(p => p, p => 0UL);
            _quorum = new HashSet<string>{_me};
            SendHeartbeats();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (watch.ElapsedMilliseconds > lastCheck + _heartbeat)
                    {
                        _raftDiagnostic?.Message("Sending heartbeats", LogLevel.Insane);
                        SendHeartbeats();
                        lastCheck = watch.ElapsedMilliseconds;
                    }

                    var (peer, message) = await _transport.Get(ct, TimeSpan.FromMilliseconds(heartbeat));
                    if (ct.IsCancellationRequested || message == null)
                        continue;

                    if (message.Term > _currentTerm)
                    {
                        _raftDiagnostic?.Message("Found higher term, switching to follower", LogLevel.Basic);
                        _state = RaftState.Follower;
                        _currentTerm = message.Term;
                        _transport.ReQueue(peer, message);
                        return;
                    }

                    switch (message)
                    {
                        case RaftCommand command:
                            _log.Append(new RaftEntry(command.Client, _currentTerm, _log.NextIndex(), command.CommandGuid, command.Command, command.Data));
                            continue;
                        case AppendEntriesResponse appendEntriesResponse:
                            if (appendEntriesResponse.Success)
                            {
                                if (appendEntriesResponse.LogIndex > _matchIndex[peer])
                                {
                                    _raftDiagnostic?.Message($"Peer '{peer}' successfully appended {appendEntriesResponse.LogIndex}", LogLevel.Basic);
                                }

                                _nextIndex[peer] = appendEntriesResponse.LogIndex + 1;
                                _matchIndex[peer] = appendEntriesResponse.LogIndex;
                                _quorum.Add(peer);
                            }
                            else
                            {
                                _raftDiagnostic?.Message($"Peer '{peer}' failed to append, peer last index is {appendEntriesResponse.LogIndex}", LogLevel.Warning);
                                _nextIndex[peer] = appendEntriesResponse.LogIndex + 1;
                                _matchIndex[peer] = appendEntriesResponse.LogIndex;
                            }

                            var quorumCount = _quorum.Count;
                            if (quorumCount << 1 > _peers.Count + 1) //Can we cache this? In the case of to many peers failing commit index will not rise.
                            {
                                _raftDiagnostic?.Message($"We have quorum, check commit {_matchIndex.Count} {quorumCount}", LogLevel.Verbose);
                                var allIndexes = _matchIndex.Values.Select(v => v).ToList();
                                allIndexes.Add(_log.LastEntry().index);
                                var quorumIndex = allIndexes.OrderBy(v => v).Skip(quorumCount - 1).First();
                                if (quorumIndex > _commitIndex && _log.GetEntry(quorumIndex).Term == _currentTerm) //TODO, om Term skijler sig?
                                {
                                    _raftDiagnostic?.Message($"We have new commit index {quorumIndex}", LogLevel.Basic);
                                    _commitIndex = quorumIndex;
                                }

                                while (_commitIndex > _lastApplied)
                                {
                                    var entry = _log.GetEntry(_lastApplied + 1);
                                    var result = _statemachine.Apply(entry.Command, entry.Data);
                                    _lastApplied++;
                                    _raftDiagnostic?.Message($"Applied {_lastApplied} goal {_commitIndex}", LogLevel.Basic);
                                    _transport.Send(entry.Client, new RaftCommandResponse(_me, entry.CommandGuid, true, result));
                                }
                            }

                            break;
                        case SnapshotRequest snapshotRequest:
                        default:
                            continue;
                    }

                }
            }
            catch (Exception ex)
            {
                _raftDiagnostic?.Message($"[LeaderLoop] We got an exception! {ex}", LogLevel.Error);
            }
        }


        private async Task CandidateLoop(CancellationToken ct)
        {
            _currentTerm += 1;
            _votedFor = _me;
            _votesReceived = new HashSet<string> { _me };
            var electionTimeout = TimeSpan.FromMilliseconds(_rand.Next(_heartbeat * 3, _heartbeat * 3 *3));
            _raftDiagnostic?.Message($"Sending vote requests to {string.Join(",", _peers)}", LogLevel.Basic);
            foreach (var peer in _peers)
            {
                var (term, index) = _log.LastEntry();
                _transport.Send(peer, new VoteRequest(_currentTerm, _me, index, term));
            }

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var (peer, message) = await _transport.Get(ct, electionTimeout);
                    if (ct.IsCancellationRequested || message == null)
                        return;
                    if (message.Term > _currentTerm || message is AppendEntriesRequest)
                    {
                        _raftDiagnostic?.Message("Found higher term, switching to follower", LogLevel.Basic);
                        _state = RaftState.Follower;
                        _currentTerm = message.Term;
                        _transport.ReQueue(peer, message);
                        return;
                    }

                    switch (message)
                    {
                        case VoteResponse voteResponse:
                        {
                            if (voteResponse.VoteGranted)
                            {
                                _raftDiagnostic?.Message($"{peer} voted for us", LogLevel.Basic);
                                _votesReceived.Add(peer);
                            }
                            else
                            {
                                _raftDiagnostic?.Message($"{peer} voted for another", LogLevel.Basic);
                            }

                            if (_votesReceived.Count << 1 > _peers.Count + 1)
                            {
                                _raftDiagnostic?.Message("We got enough votes, switching to leader", LogLevel.Basic);
                                _state = RaftState.Leader;
                                return;
                            }

                            break;
                        }
                        case VoteRequest _:
                            _transport.Send(peer, new VoteResponse(_currentTerm, false));
                            break;

                        case RaftCommand command:
                            //Leader election in progress, should we await result or tell the client to retry? 
                            //Depends on if the client should try other peers automatically or only when redirected by response?
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                _raftDiagnostic?.Message($"[CandidateLoop] We got an exception! {ex}", LogLevel.Error);
            }
        }

        private async Task FollowerLoop(CancellationToken ct)
        {
            var electionTimeout = TimeSpan.FromMilliseconds(_rand.Next(_heartbeat * 3, _heartbeat * 3 * 3));
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var (peer, message) = await _transport.Get(ct, electionTimeout);
                    if (ct.IsCancellationRequested)
                        return;

                    if (message == null)
                    {
                        _state = RaftState.Candidate;
                        return;
                    }

                    switch (message)
                    {
                        case VoteRequest voteRequest:
                            if (voteRequest.Term < _currentTerm)
                            {
                                _raftDiagnostic?.Message($"Deny vote request from {peer}, we have higher term", LogLevel.Basic);
                                _transport.Send(peer, new VoteResponse(_currentTerm, false));
                                continue;
                            }

                            if ((_votedFor == null || _votedFor == peer) && voteRequest.LastLogIndex >= _log.LastEntry().index)
                            {
                                _raftDiagnostic?.Message($"Vote for {peer}", LogLevel.Basic);
                                _votedFor = peer;
                                _transport.Send(peer, new VoteResponse(_currentTerm, true));
                                continue;
                            }
                            else
                            {
                                _raftDiagnostic?.Message($"Deny vote request from {peer}, we have already voted for {_votedFor}", LogLevel.Basic);
                                _transport.Send(peer, new VoteResponse(_currentTerm, false));
                                continue;
                            }
                        case AppendEntriesRequest appendEntriesRequest:
                            if (appendEntriesRequest.Term < _currentTerm)
                            {
                                _raftDiagnostic?.Message($"Deny append entries request from {peer}, we have higher term", LogLevel.Basic);
                                _transport.Send(peer, new AppendEntriesResponse(_currentTerm, false, _log.LastEntry().index));
                                continue;
                            }

                            _leader = peer;
                            if (appendEntriesRequest.Entries.Count == 0)
                            {
                                _transport.Send(peer, new AppendEntriesResponse(_currentTerm, true, _log.LastEntry().index));
                                continue;
                            }

                            if (!_log.HasEntry(appendEntriesRequest.PrevLogTerm, appendEntriesRequest.PrevLogIndex))
                            {
                                _raftDiagnostic?.Message($"We cannot append entries from {peer}, our log is to old", LogLevel.Warning);
                                _transport.Send(peer, new AppendEntriesResponse(_currentTerm, false, _log.LastEntry().index));
                                continue;
                            }

                            foreach (var entry in appendEntriesRequest.Entries)
                            {
                                _log.Append(entry);
                            }

                            _transport.Send(peer, new AppendEntriesResponse(_currentTerm, true, _log.LastEntry().index));

                            if (appendEntriesRequest.LeaderCommit > _commitIndex)
                            {
                                _commitIndex = Math.Min(appendEntriesRequest.LeaderCommit, _log.LastEntry().index);
                                while (_commitIndex > _lastApplied)
                                {
                                    var entry = _log.GetEntry(_lastApplied + 1);
                                    _raftDiagnostic?.Message($"Applying {entry.LogIndex}", LogLevel.Basic);
                                    _statemachine.Apply(entry.Command, entry.Data);
                                    _lastApplied++;
                                }
                            }

                            break;
                        case RaftCommand _:
                            _transport.Send(peer, new RaftCommandResponse(_leader, null, false, null));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _raftDiagnostic?.Message($"[FollowerLoop] We got an exception! {ex}", LogLevel.Error);
            }
        }

        private void SendHeartbeats()
        {
            try
            {
                var index = _log.LastEntry().index;
                foreach (var peer in _peers)
                {
                    var entries = _nextIndex.TryGetValue(peer, out var fromIndex) ? _log.GetEntries(fromIndex, index, 50) : new List<IRaftEntry>();
                    if (entries.Any())
                    {
                        _raftDiagnostic?.Message($"Sending {entries.Count} entries to {peer}", LogLevel.Basic);
                    }

                    _transport.Send(peer, new AppendEntriesRequest(_currentTerm, _me, 0, 0, _commitIndex, entries));
                }
            }
            catch (Exception ex)
            {
                _raftDiagnostic?.Message($"[SendHeartbeats] We got an exception! {ex}", LogLevel.Error);
            }

        }
    }
}
