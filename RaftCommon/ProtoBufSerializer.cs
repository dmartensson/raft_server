using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RaftServer
{
    public class ProtoBufSerializer : IRaftSerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            using (var m = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(m, obj);
                return m.ToArray();
            }
        }

        public T Deserialize<T>(byte[] data)
        {
            using (var m = new MemoryStream(data))
            {
                return ProtoBuf.Serializer.Deserialize<T>(m);
            }
        }
    }
}
