using System;
using System.Collections.Generic;
using System.Text;

namespace RaftServer
{
    interface IRaftTiming
    {
        int GetHeartbeat();
        int GetElectionTimeout();
    }
}
