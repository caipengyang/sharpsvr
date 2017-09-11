using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using sharpsvr.common;
using sharpsvr.exception;

namespace sharpsvr.net
{
    public class SocketClient
    {

        private static log4net.ILog log = Logger.GetInstance().GetLog();

        public Int32 ConnectFailCount;

        protected SocketSession clientSession;

        public SocketClient(string ip = "127.0.0.1", short port = 10086)
        {
            clientSession = new ClientSocketSession();
        }

        public void Connect(string ip = "127.0.0.1", short port = 10086)
        {
            if(clientSession.IsConnected) return;

            if (!string.IsNullOrEmpty(SharpSvrSettings.GetInstance().ConnectIp))
            {
                if ("127.0.0.1".Equals(ip)) ip = SharpSvrSettings.GetInstance().ConnectIp;
            }
            if (SharpSvrSettings.GetInstance().ConnectPort > 0)
            {
                if (port == 10086) port = SharpSvrSettings.GetInstance().ConnectPort;
            }
            EndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = socket.ReceiveTimeout = SharpSvrSettings.GetInstance().ClientSendRecvTimeUnit;
            try{
                socket.Connect(endPoint);
                clientSession.WithSocket(socket);
                Interlocked.Exchange(ref ConnectFailCount, 0);
            }catch(SocketException se){
                Interlocked.Add(ref ConnectFailCount, 1);
                throw se;
            }
        }

        public void Disconnect()
        {
            if(!clientSession.IsConnected) return;
            clientSession.Close();
        }
        public protocol.SharpPacket SendAndRecvPacket(MethodInfo methodInfo, object[] args)
        {
            var contentBytes = protocol.SharpProtocol.GetInstance().Encode(methodInfo, args);
            clientSession.Send(protocol.SharpProtocol.GetHeader(contentBytes));
            clientSession.Send(contentBytes);
            var timeOutTime = SharpSvrSettings.GetInstance().ClientRecvTimeout;
            var waitTime = 0L;
            while (waitTime < timeOutTime)
            {
                long start = DateTime.Now.Millisecond;
                if (clientSession.BindSocket.Poll(10000, SelectMode.SelectRead | SelectMode.SelectWrite))
                {
                    clientSession.CanRead = true;
                    clientSession.CanWrite = true;
                    clientSession.DoSend();
                    clientSession.DoRecv();
                    if (clientSession.IsError)
                    {
                        clientSession.Close();
                        string message = $"server error occured! when invoke method {methodInfo.Name} with arguments {args}";
                        log.Error(message);
                        return new protocol.SharpPacket { Result = new WrapServerException(message) };
                    }
                    var packet = protocol.SharpProtocol.GetInstance().TryDecodePacket(clientSession);
                    if (packet != null)
                    {
                        //log.Debug("recv packet:" + packet.Random);
                        return packet;
                    }
                    waitTime = (DateTime.Now.Millisecond - start) * 1000;
                }
            }
            return null;
        }
    }
}