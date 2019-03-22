namespace RaftServer
{
    public class RaftCommand : RaftMessage
    {
        public RaftCommand(string client, byte[] commandGuid, string command, byte[] data) : base(0)
        {
            CommandGuid = commandGuid;
            Command = command;
            Data = data;
            Client = client;
        }

        public byte[] CommandGuid { get; }
        public byte[] Data { get; }
        public string Command { get; }
        public string Client { get; }
    }
}