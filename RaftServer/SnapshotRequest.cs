namespace RaftServer
{
    internal class SnapshotRequest : RaftMessage
    {
        public SnapshotRequest() : base(0)
        {
        }
    }
}