using System;
using System.Threading.Tasks;
using commontest.src.test;
using sharpsvr.net;

namespace svrtest
{
    class Program
    {
        static void Main(string[] args)
        {
            var svr = new sharpsvr.net.SharpServer();
            ICalc cal = new Calc();
            Action<IServer> action = async (IServer server)=>{
                svr.WithService(cal);
                svr.StartUp();
                await Task.Run(()=> server.MainLoop());
            };
            action(svr);
            Console.ReadKey();
            svr.ShutDown();
        }
    }
}
