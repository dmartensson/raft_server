using System.Collections;
using System.Text;

namespace RaftServer
{
    public class RaftMessage
    {
        public RaftMessage(uint term)
        {
            Term = term;
        }
        public uint Term { get; }
    }
}
