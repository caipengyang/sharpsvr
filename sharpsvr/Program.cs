using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using log4net;
using sharpsvr.attributes;
using sharpsvr.common;
using sharpsvr.core;
using sharpsvr.net;
using sharpsvr.proxy;

namespace sharpsvr
{
    class Program
    {
        static ILog log = Logger.GetInstance().GetLog();

        class A { }

        static void RunServer()
        {
            var server = new TcpServer();
            server.StartUp();
            Action<IServer> MainLoop = async (IServer svr) =>
            {
                await Task.Run(() => { svr.MainLoop(); });
            };
            MainLoop(server);
            Console.ReadKey();
            server.ShutDown();
        }

        static void TestSerialize(){
            var packet = new protocol.SharpPacket{
                Random=1,
                Version = "12.0.32",
                MethodUniqueName = "test",
                ServiceUniqueName = "te",
                Result = new Exception("test"),
                Arguments = new object[]{1, "2", null}
            };

            var serialize = protocol.SerializerManager.GetInstance().GetSerializer();
            var bytes = serialize.Serialize(packet);
            log.Debug("deserialize :" + serialize.Deserialize<protocol.SharpPacket>(bytes));
        }

        public static void Main(string[] args)
        {
            TestSerialize();
        }
    }
}
