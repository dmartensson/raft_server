namespace RaftServer
{
    public class RaftCommandResponse : RaftMessage
    {
        public bool Success { get; }
        public byte[] CommandGuid { get; }
        public byte[] Result { get; }
        public string Leader { get; }

        public RaftCommandResponse(string leader, byte[] commandGuid, bool success, byte[] result) : base(0)
        {
            CommandGuid = commandGuid;
            Success = success;
            Result = result;
            Leader = leader;
        }
    }
}