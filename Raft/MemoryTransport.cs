﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RaftServer;

namespace Raft
{
    public class MemoryTransport
    {
        private readonly Dictionary<string, AsyncQueue<(string peer, RaftMessage message)>> _buckets = new Dictionary<string, AsyncQueue<(string peer, RaftMessage message)>>();
        private readonly IRaftDiagnostic _raftDiagnostic;

        public MemoryTransport(IRaftDiagnostic raftDiagnostic = null)
        {
            _raftDiagnostic = raftDiagnostic ?? new DummyDiagnostics();
        }
        private AsyncQueue<(string peer, RaftMessage message)> GetBucket(string peer)
        {
            if (!_buckets.TryGetValue(peer, out var bucket))
            {
                bucket = new AsyncQueue<(string peer, RaftMessage message)>();
                _buckets.Add(peer, bucket);
            }
            return bucket;
        }

        public IRaftTransport GetPeerTransport(string me)
        {
            return new PeerTransport(me, GetBucket(me), GetBucket, _raftDiagnostic);
        }

        private class PeerTransport : IRaftTransport
        {
            private readonly AsyncQueue<(string peer, RaftMessage message)> _queue;
            private readonly Queue<(string peer, RaftMessage)> _requeue = new Queue<(string peer, RaftMessage)>();
            private readonly Func<string, AsyncQueue<(string, RaftMessage)>> _getBucket;
            private readonly string _me;
            private readonly IRaftDiagnostic _raftDiagnostic;

            public PeerTransport(string me, AsyncQueue<(string peer, RaftMessage message)> queue, Func<string, AsyncQueue<(string, RaftMessage)>> getBucket, IRaftDiagnostic raftDiagnostic = null)
            {
                _queue = queue;
                _getBucket = getBucket;
                _me = me;
                _raftDiagnostic = raftDiagnostic;
            }
            public void Send(string peer, RaftMessage message)
            {
                _raftDiagnostic?.Message($"Message of type {message.GetType().Name} from '{_me}' to '{peer}'", RaftCommon.LogLevel.Insane);
                _getBucket(peer).Enqueue((_me, message));
            }

            public async Task<(string peer, RaftMessage message)> Get(CancellationToken ct, TimeSpan timeout)
            {
                _raftDiagnostic?.Message($"{_me} waiting for message", RaftCommon.LogLevel.Insane);
                if (_requeue.Count > 0)
                {
                    return await Task.FromResult(_requeue.Dequeue());
                }
                try
                {
                    return await _queue.DequeueAsync((int)timeout.TotalMilliseconds, ct);

                }
                catch (TaskCanceledException)
                {
                    return await Task.FromResult<(string peer, RaftMessage message)>((null, null));
                }
            }

            public void ReQueue(string peer, RaftMessage message)
            {
                _requeue.Enqueue((peer, message));
            }
        }
    }
}
