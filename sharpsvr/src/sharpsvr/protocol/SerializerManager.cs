using System;
using sharpsvr.common;
using sharpsvr.protocol.serializer;

namespace sharpsvr.protocol
{
    public class SerializerManager : Singleton<SerializerManager>
    {

        private volatile ISerializable serializer;
        public ISerializable GetSerializer()
        {
            if (serializer != null) return serializer;
            var serializerType = SharpSvrSettings.GetInstance().SerializerType;
            if ("json".Equals(serializerType.ToLower()) || typeof(JsonSerializer).FullName.Equals(serializerType))
            {
                serializer = JsonSerializer.GetInstance();
            }
            else if ("binary".Equals(serializerType.ToLower()) || typeof(BinarySerializer).FullName.Equals(serializerType))
            {
                serializer = BinarySerializer.GetInstance();
            }
            else
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.FullName.Equals(serializerType))
                        {
                            serializer = Activator.CreateInstance(type) as ISerializable;
                            break;
                        }
                    }
                }
                if (serializer == null) serializer = JsonSerializer.GetInstance();
            }
            return serializer;
        }
    }
}