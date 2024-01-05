# sharpsvr

## what is sharpsvr

sharpsvr 是一个企业级的dotnetcore rpc框架,最新版本基于dotnetcore 2.0开发, 支持
符合dotnetcore all platforms that support dotnetcore environments.传统企业级服务通过只需引用sharpsvr库简单的改造即可变成
分布式服务.sharpsvr是一个轻量级\性能优秀的框架.可以同时支持多达上千个客户端链接提供
稳定的服务支持.sharpsvr计划陆续支持"远程调用"\"服务发现"\"服务版本"\"服务升级降级"\"服务路由"\"自动节点检查"等
功能.

>简单demo:

+ 服务接口定义: ICalc.cs

``` csharp
using System.Collections.Generic;
using sharpsvr.attributes;

namespace commontest.src.test
{
    [RemoteService]
    public interface ICalc
    {
        [RemoteMethod]
        void SayHello(string name="yangyang");

        [RemoteMethod]
        int TestA(short b);

        [RemoteMethod]
        int TestA(int c, long d);

        [RemoteMethod]
        double Add(int a, long b, float c=1.0f, double d = 2.0);

        [RemoteMethod]
        List<User> GetUserList(User user = null);

        [RemoteMethod]
        User GetUser(User a=null, User b=null);

    }
}
```

+ 服务实现

```csharp
using System.Collections.Generic;

namespace commontest.src.test
{
    public class Calc : ICalc
    {
        public double Add(int a, long b, float c = 1, double d = 2)
        {
            return a + b + c + d;
        }

        public User GetUser(User a = null, User b = null)
        {
            System.Console.WriteLine($"a={a}, b={b}");
            throw new System.NotImplementedException();
        }

        public List<User> GetUserList(User user = null)
        {
            var result = new List<User>();
            result.Add(user);
            result.Add(new User{Id=1, Age=2, Sex=true, Message = "hello,hahah", Child=user});
            return result;
        }

        public void SayHello(string name = "yangyang")
        {
            System.Console.WriteLine("hello:" + name);
        }

        public int TestA(short b)
        {
            return b++;
        }

        public int TestA(int c, long d)
        {
            return (int)(c * d);
        }
    }
}
```

+ server端实现

``` csharp
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
```

+ 客户端调用

```csharp
﻿using System;
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

```

## v 0.1.0

以下是sharpsvr 0.1.0 支持的功能:

+ 分布式服务.
+ 服务名称发现\服务版本支持
+ 故障节点剔除\重连
+ 长链接\短链接
+ 通用的tcp\udp协议支持,自定义编码解码器支持
