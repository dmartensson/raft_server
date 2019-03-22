namespace RaftServer
{
    public class VoteResponse : RaftMessage
    {
        public VoteResponse(uint term, bool voteGranted) : base(term)
        {
            VoteGranted = voteGranted;
        }

        public bool VoteGranted { get; }
    }
}