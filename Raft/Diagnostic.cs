using System;
using System.Diagnostics;
using RaftCommon;
using RaftServer;

namespace Raft
{
    class Diagnostic : IRaftDiagnostic {
        private readonly string _me;
        private readonly Stopwatch _watch;
        private readonly LogLevel _logLevel;

        public Diagnostic(string me, Stopwatch watch = null, LogLevel logLevel = LogLevel.Warning)
        {
            _me = me;
            _watch = watch ?? Stopwatch.StartNew();
            Console.WriteLine($"Diagnostic for {_me}");
            _logLevel = logLevel;
        }
        public void LoadedConfig(RaftConfig config)
        {
            Console.WriteLine($"({_watch.ElapsedMilliseconds}){_me}-config: {config.Me}, {string.Join(",", config.Peers)}");

        }

        public void EnterState(RaftState state)
        {
            Console.WriteLine($"({_watch.ElapsedMilliseconds}){_me}-state: {Enum.GetName(typeof(RaftState), state)}");
        }

        public void Message(string message, LogLevel loglevel)
        {
            if ((int)loglevel <= (int)_logLevel)
            {
                Console.WriteLine($"({_watch.ElapsedMilliseconds}){_me}: {message}");
            }
        }
    }
}