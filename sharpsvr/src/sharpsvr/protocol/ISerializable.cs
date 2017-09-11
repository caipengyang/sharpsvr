namespace sharpsvr.protocol
{
    public interface ISerializable
    {

        byte[] Serialize(object obj);
        
        T Deserialize<T>(byte[] bytes, int from=0, int to=0) where T : class;
         
    }
}