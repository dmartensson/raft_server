using RaftCommon;

namespace RaftServer
{
    public interface IRaftDiagnostic
    {
        void LoadedConfig(RaftConfig config);
        void EnterState(RaftState state);
        void Message(string message, LogLevel loglevel);
    }
}