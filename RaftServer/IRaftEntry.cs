namespace RaftServer
{
    public interface IRaftEntry
    {
        string Command { get; }
        byte[] Data { get; }
        string Client { get; }
        uint Term { get; }
        ulong LogIndex { get; }
        byte[] CommandGuid { get; }
    }
}