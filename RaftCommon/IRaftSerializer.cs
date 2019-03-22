namespace RaftServer
{
    public interface IRaftSerializer
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] data);
    }
}