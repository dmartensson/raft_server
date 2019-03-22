using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RaftServer
{
    class HttpTransport : IRaftTransport, IDisposable
    {
        private Queue<RaftMessage> _received = new Queue<RaftMessage>();
        public HttpTransport(IPAddress address = null, int port = 0)
        {
            
        }
        public void Send(string peer, RaftMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<(string peer, RaftMessage message)> Get(CancellationToken ct, TimeSpan timeout)
        {
            //, Task.Delay(electionTimeout), Task.FromCanceled(ct)
            throw new NotImplementedException();
        }

        public void ReQueue(string peer, RaftMessage message)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}