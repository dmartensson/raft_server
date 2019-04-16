using System;
using System.Collections.Generic;
using RaftServer;

namespace Raft
{
    class MemoryStorage : IRaftStorage
    {
        private readonly Dictionary<string, byte[]> _storage = new Dictionary<string, byte[]>();
        public void Write(string name, byte[] data)
        {
            _storage.Add(name, data);
        }

        public byte[] Read(string name)
        {
            return _storage.TryGetValue(name, out var data) ? data : null;
        }

        public void Append(string name, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void Delete(string name)
        {
            throw new NotImplementedException();
        }
    }
}