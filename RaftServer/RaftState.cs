namespace RaftServer
{
    public enum RaftState
    {
        Offline,
        Follower,
        Candidate,
        Leader,
        Snapshot
    }
}