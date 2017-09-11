using log4net;
using sharpsvr.common;

namespace sharpsvr.core
{
    public class ByteArrayFactory : Singleton<ByteArrayFactory>
    {

        private static ILog log = Logger.GetInstance().GetLog();
        public byte[] allocate(uint size){
            log.Debug($"allocating byte buffer: {size}");
            return new byte[size];
        }

        public void recycle(byte[] bytes){
            log.Debug($"recyle byte buffer{bytes.Length}");
        }
    }
}