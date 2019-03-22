using System.Collections.Generic;

namespace RaftServer
{
    public class AppendEntriesRequest : RaftMessage
    {
        public AppendEntriesRequest(uint term, string leaderId, ulong prevLogindex, uint prevLogTerm, ulong commitIndex, List<IRaftEntry> entries = null) : base(term)
        {
            LeaderId = leaderId;
            PrevLogIndex = prevLogindex;
            PrevLogTerm = prevLogTerm;
            LeaderCommit = commitIndex;
            Entries = entries;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string LeaderId { get; }
        public uint PrevLogTerm { get; }
        public ulong PrevLogIndex { get; }
        public List<IRaftEntry> Entries { get; }
        public ulong LeaderCommit { get; }
    }
}