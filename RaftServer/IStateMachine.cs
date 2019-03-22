using System;
using System.Collections.Generic;

namespace RaftServer
{
    public interface IStateMachine
    {
        byte[] Apply(string command, byte[] data);
        void RegisterCommand(string command, Func<byte[], byte[]> action);
    }

    public class StateMachine : IStateMachine
    {
        private Dictionary<string, Func<byte[], byte[]>> _externalCommands = new Dictionary<string, Func<byte[],byte[]>>();
        public byte[] Apply(string command, byte[] data)
        {
            if (_externalCommands.TryGetValue(command, out var action))
            {
                return action(data);
            }

            throw new NotImplementedException();
        }

        public void RegisterCommand(string command, Func<byte[], byte[]> action)
        {
            _externalCommands[command] = action;
        }
    }
}