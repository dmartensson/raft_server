namespace RaftServer
{
    public class RaftEntry : IRaftEntry
    {
        public string Command { get; }
        public byte[] Data { get; }
        public string Client { get; }
        public uint Term { get; }
        public ulong LogIndex { get; }
        public byte[] CommandGuid { get; }

        public RaftEntry(string client, uint term, ulong logIndex, byte[] commandGuid, string command, byte[] data)
        {
            Command = command;
            CommandGuid = commandGuid;
            Data = data;
            Client = client;
            Term = term;
            LogIndex = logIndex;
        }

    }
}