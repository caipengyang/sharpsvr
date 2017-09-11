using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using sharpsvr.common;

namespace sharpsvr.protocol.serializer
{
    public class BinarySerializer : Singleton<BinarySerializer>, ISerializable
    {
        ThreadLocal<BinaryFormatter> formatter = new ThreadLocal<BinaryFormatter>();

        ThreadLocal<MemoryStream> stream = new ThreadLocal<MemoryStream>();

        private BinaryFormatter GetFormatter()
        {
            return formatter.IsValueCreated ? formatter.Value : formatter.Value = new BinaryFormatter();
        }

        private MemoryStream GetStream()
        {
            return stream.IsValueCreated ? stream.Value : stream.Value = new MemoryStream();
        }

        public T  Deserialize<T>(byte[] bytes, int from = 0, int to = 0) where T: class
        {
            if (bytes == null || bytes.Length <= 0) return null;
            if (to <= 0) to = bytes.Length;
            if (to <= from) return null;
            var stream = GetStream();
            stream.SetLength(0);
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(bytes, from, to - from);
            stream.Seek(0, SeekOrigin.Begin);
            return GetFormatter().Deserialize(stream) as T;
        }

        public byte[] Serialize(object obj)
        {
            var stream = GetStream();
            stream.Seek(0, SeekOrigin.Begin);
            stream.SetLength(0);
            GetFormatter().Serialize(stream, obj);
            return stream.ToArray();
        }
    }
}