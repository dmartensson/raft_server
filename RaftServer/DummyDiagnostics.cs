using RaftCommon;

namespace RaftServer
{
    public class DummyDiagnostics : IRaftDiagnostic {

        public void LoadedConfig(RaftConfig config)
        {
        }

        public void EnterState(RaftState state)
        {
        }

        public void Message(string message, LogLevel loglevel)
        {
        }
    }
}