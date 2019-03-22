namespace RaftServer
{
    public class AppendEntriesResponse : RaftMessage
    {

        public AppendEntriesResponse(uint term, bool success, ulong logIndex) : base(term)
        {
            Success = success;
            LogIndex = logIndex;
        }

        public bool Success { get; }
        public ulong LogIndex { get; }
    }
}