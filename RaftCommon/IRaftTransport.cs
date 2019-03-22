using System;
using System.Threading;
using System.Threading.Tasks;

namespace RaftServer
{
    public interface IRaftTransport
    {
        void Send(string peer, RaftMessage message);
        Task<(string peer, RaftMessage message)> Get(CancellationToken ct, TimeSpan timeout);
        void ReQueue(string peer, RaftMessage message);
    }
}