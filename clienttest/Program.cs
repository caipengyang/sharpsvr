using System;
using System.Threading.Tasks;
using commontest.src.test;

namespace clienttest
{

    class Program
    {
        static void RunAsync()
        {
            Action<Int32> action = async (Int32 number) =>
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Console.WriteLine("thread start number:" + number + ", current thread:" + System.Threading.Thread.CurrentThread.GetHashCode());
                        ICalc proxy = sharpsvr.proxy.ProxyGenerator.Of<ICalc>(new sharpsvr.proxy.SharpInterceptor());
                        while (true)
                        {
                            try
                            {
                                Console.WriteLine("hello:" + proxy.TestA(1, 2L));
                                var userList = proxy.GetUserList(new User { Id = 123 });
                                foreach (var user in userList) Console.WriteLine("hello:" + user);
                                Console.WriteLine("proxy.GetUser:" + proxy.GetUser(new User { Id = 1 }, new User { Age = 2 }));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("error exception:" + ex);
                            }
                        }
                    }
                    finally
                    {
                        Console.WriteLine("exception not catched!");
                    }
                });


            };

            for (int i = 0; i < 100; ++i)
            {
                action(i);
            }

        }

        static void RunSync()
        {
            try
            {
                ICalc proxy = sharpsvr.proxy.ProxyGenerator.Of<ICalc>(new sharpsvr.proxy.SharpInterceptor());
                while (true)
                {
                    try
                    {
                        Console.WriteLine("hello:" + proxy.TestA(1, 2L));
                        var userList = proxy.GetUserList(new User { Id = 123 });
                        foreach (var user in userList) Console.WriteLine("hello:" + user);
                        Console.WriteLine("proxy.GetUser:" + proxy.GetUser(new User { Id = 1 }, new User { Age = 2 }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("error exception:" + ex);
                    }
                }
            }
            finally
            {
                Console.WriteLine("exception not catched!");
            }

        }

        static void Main(string[] args)
        {
            RunAsync();
            Console.ReadKey();
            //while(true) System.Threading.Thread.Sleep(100000);
            Environment.Exit(0);
        }
    }
}
