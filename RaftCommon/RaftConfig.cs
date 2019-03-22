using System.Collections.Generic;

namespace RaftServer
{
    public class RaftConfig
    {
        public RaftConfig(List<string> peers, string me = null, int? heartbeat = null)
        {
            Peers = peers;
            Me = me;
            Heartbeat = heartbeat;
        }
        public List<string> Peers { get; }
        public string Me { get; }
        public int? Heartbeat { get; }
    }
}