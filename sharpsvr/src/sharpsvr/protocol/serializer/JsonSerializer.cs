using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using sharpsvr.common;
using Newtonsoft.Json;

namespace sharpsvr.protocol.serializer
{
    public class JsonSerializer : Singleton<JsonSerializer>, ISerializable
    {
        public T Deserialize<T>(byte[] bytes, int from = 0, int to = 0) where T : class
        {
            if (bytes == null || bytes.Length <= 0) return null;
            if (to <= 0) to = bytes.Length;
            if (to <= from) return null;
            return JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(bytes, from, to - from), typeof(T)) as T;
        }

        public byte[] Serialize(object obj)
        {
            return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }
    }
}