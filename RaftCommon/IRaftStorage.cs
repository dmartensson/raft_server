using System;

namespace RaftServer
{
    public interface IRaftStorage
    {
        void Write(string name, byte[] data);
        byte[] Read(string name);
        void Append(string name, byte[] data);
        void Delete(string name);
    }
}