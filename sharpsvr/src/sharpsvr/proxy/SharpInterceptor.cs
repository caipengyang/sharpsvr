using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using sharpsvr.common;
using sharpsvr.net;
using sharpsvr.util;

namespace sharpsvr.proxy
{
    public class SharpInterceptor : Interceptor
    {
        private class SocketClientFactory
        {

            private static log4net.ILog log = Logger.GetInstance().GetLog();

            private Random random = new Random();
            private volatile ConcurrentDictionary<string, SocketClient> allSockets = new ConcurrentDictionary<string, SocketClient>();
            public SocketClient GetSocketClient(Type type)
            {
                var settings = SharpSvrSettings.GetInstance();
                string name = null;
                string ip = null;
                short port = 0;
                int retryCount = 0;
                SocketClient socket = null;
                while (retryCount++ < SharpSvrSettings.GetInstance().MaxConnectFailCount)
                {
                    if (settings.Type2AllService.ContainsKey(type))
                    {
                        var serviceConfig = settings.Type2AllService[type];
                        var len = serviceConfig.ServerList.Count;
                        var server = serviceConfig.ServerList[random.Next(len)];
                        name = serviceConfig.ServiceName + "@" + server.ServerName;
                        ip = server.ServerIp;
                        port = server.ServerPort;
                    }
                    else
                    {
                        name = "default";
                        ip = SharpSvrSettings.GetInstance().ConnectIp;
                        port = SharpSvrSettings.GetInstance().ConnectPort;
                    }
                    try
                    {
                        if (!allSockets.ContainsKey(name))
                        {
                            socket = allSockets[name] = new SocketClient(ip, port);
                            if (SharpSvrSettings.GetInstance().ClientKeepAlive)
                            {
                                socket.Connect(ip, port);
                            }
                        }
                        else
                        {
                            socket = allSockets[name];
                            socket.Connect(ip, port);
                        }
                        break;
                    }
                    catch (SocketException se)
                    {
                        if (socket.ConnectFailCount > SharpSvrSettings.GetInstance().MaxConnectFailCount)
                        {
                            log.Error($"connect to {ip}:{port} failed! going to remove endpoint={name}", se);
                            SocketClient socketClient = null;
                            allSockets.Remove(name, out socketClient);
                            socket = null;
                        }

                    }
                }
                return socket;
            }
        }

        private static log4net.ILog log = Logger.GetInstance().GetLog();

        private SocketClientFactory socketClientFactory = new SocketClientFactory();
        public SharpInterceptor()
        {
        }
        public override object Invoke(MethodInfo methodInfo, object[] args)
        {
            SocketClient socket = null;
            try
            {
                socket = socketClientFactory.GetSocketClient(methodInfo.DeclaringType);
                if (socket == null)
                {
                    throw new exception.SharpServerException($"service for {methodInfo.DeclaringType.FullName} not found!");
                }
                var packet = socket.SendAndRecvPacket(methodInfo, args);
                var result = packet?.Result;
                if (result != null && result is exception.WrapServerException) throw new exception.SharpServerException((result as exception.WrapServerException).Message); ;
                return ConvertUtils.ConvertResultType(methodInfo, result);
            }
            finally
            {
                if (!SharpSvrSettings.GetInstance().ClientKeepAlive) socket.Disconnect();
            }
        }
    }
}