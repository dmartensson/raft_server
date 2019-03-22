using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RaftServer;

namespace RaftClient
{
    // ReSharper disable once UnusedMember.Global
    public class RaftClient
    {
        private readonly IRaftTransport _transport;
        private List<string> _servers;
        private string _leader;
        private readonly string _me;
        private readonly IRaftSerializer _raftSerializer;
        private readonly int _heartbeat;

        public RaftClient(IRaftTransport transport, IRaftSerializer raftSerializer, List<string> servers, string me = null, int heartbeat = 50)
        {
            _transport = transport;
            _raftSerializer = raftSerializer;
            _servers = servers;
            _me = me ?? Guid.NewGuid().ToString();
            _heartbeat = heartbeat;
        }

        // ReSharper disable once UnusedMember.Global
        public async Task<(bool success, byte[] data)> Command(string command, byte[] data, CancellationToken ct)
        {
            var guid = Guid.NewGuid().ToByteArray();
            var commandSent = false;
            while (!ct.IsCancellationRequested)
            {
                if (!commandSent)
                {
                    _transport.Send(_leader, new RaftCommand(_me, guid, command, data));
                    commandSent = true;
                }
                var (_, message) = await _transport.Get(ct, TimeSpan.FromHours(1));
                if (message is RaftCommandResponse response)
                {
                    if (!response.Success && response.Leader != _leader)
                    {
                        _leader = response.Leader;
                        commandSent = false;
                        continue;
                    }

                    return (response.Success, response.Result);
                }
            }

            return (false, null);
        }

        public async Task Connect(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_leader))
            {
                _leader = _servers.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(_leader))
                {
                    throw new InvalidOperationException("Serverlist is empty!");
                }
            }
            while (true)
            {
                _transport.Send(_leader, new RaftCommand(_me, Guid.NewGuid().ToByteArray(), "peers", null));

                var (_, message) = await _transport.Get(ct, TimeSpan.FromSeconds(60));
                if (message == null)
                {
                    throw new InvalidOperationException("Connection timed out!");
                }
                if (!(message is RaftCommandResponse raftCommandResponse))
                {
                    throw new InvalidOperationException("Illegal response type");
                }

                if (raftCommandResponse.Success)
                {
                    _servers = _raftSerializer.Deserialize<List<string>>(raftCommandResponse.Result);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(raftCommandResponse.Leader))
                {
                    _leader = raftCommandResponse.Leader;
                }

                try
                {
                    await Task.Delay(_heartbeat * 9, ct);
                }
                catch(OperationCanceledException)
                { }
            }
        }
    }
}
