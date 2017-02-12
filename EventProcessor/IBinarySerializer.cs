using ProtoBuf;
using System.IO;

namespace EventProcessor
{
    public interface IBinarySerializer
    {
        byte[] Serialize<T>(T item);
        T Deserialize<T>(byte[] bytes);
    }

    internal class BinarySerializer : IBinarySerializer
    {
        public byte[] Serialize<T>(T item)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, item);
                return ms.ToArray();
            }
        }

        public T Deserialize<T>(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                ms.Position = 0;
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}