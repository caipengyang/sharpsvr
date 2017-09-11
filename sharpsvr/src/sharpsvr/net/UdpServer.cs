using System.Net;
using System.Net.Sockets;
using sharpsvr.common;
using sharpsvr.core;

namespace sharpsvr.net
{
    public class UdpServer : SocketServer
    {
        private static log4net.ILog log = Logger.GetInstance().GetLog();

        private IPEndPoint EndPoint { get; set; }

        public UdpServer() : base()
        {
        }

        protected override void BeforeRecvData(SocketSession session, byte[] bytes, uint from, uint to)
        {
            log.Debug($"receive data:{System.Text.Encoding.UTF8.GetString(bytes, (int)from, (int)(from + to)).TrimEnd()}");
        }

        protected override void BeforeSendData(SocketSession session, byte[] bytes, uint from, uint to)
        {
            log.Debug($"send data:{System.Text.Encoding.UTF8.GetString(bytes, (int)from, (int)(from + to))}");
        }

        protected override void OnConnected(SocketSession session)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnDisConnected(SocketSession session)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnMainLoop()
        {
            try
            {
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                var len = serverSession.DoRecvFrom(endPoint);
                if (len > 0)
                {
                    log.Info($"udp has received {len} data.");
                }
            }
            catch (SocketException ex)
            {
                if (ex.Message.Contains("timed out")) return;
                else
                {
                    log.Error("main loop error!", ex);
                    throw ex;
                }
            }
        }

        protected override void OnRecvData(SocketSession session, uint len)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnSendData(SocketSession session, uint len)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnShutDown()
        {
            serverSession.Close();
        }

        protected override void OnStartUp(string ip, short port)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //UdpClient client = new UdpClient(EndPoint);
            server.Bind(EndPoint);
            server.ReceiveTimeout = (int)SharpSvrSettings.GetInstance().UdpRecvTimeOut;
            serverSession.WithSocket(server);
            serverSession.OnSend += (ref byte[] bytes, ref uint from, ref uint to) =>
                {
                    BeforeSendData(serverSession, bytes, from, to);
                };
            serverSession.OnRecv += (ref byte[] bytes, ref uint from, ref uint to) =>
                {
                    BeforeRecvData(serverSession, bytes, from, to);
                };
        }
    }
}