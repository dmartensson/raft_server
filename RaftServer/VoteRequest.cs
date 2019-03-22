namespace RaftServer
{
    public class VoteRequest : RaftMessage
    {
        public VoteRequest(uint term, string candidateId, ulong lastLogIndex, uint lastLogTerm) : base(term)
        {
            LastLogIndex = lastLogIndex;
            CandidateId = candidateId;
            LastLogTerm = lastLogTerm;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public uint LastLogTerm { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string CandidateId { get; }

        public ulong LastLogIndex { get; }
    }
}