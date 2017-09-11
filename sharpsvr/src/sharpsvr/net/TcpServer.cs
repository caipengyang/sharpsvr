using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using sharpsvr.common;

namespace sharpsvr.net
{
    public class TcpServer : SocketServer
    {
        public delegate void SessionEventDelegate(SocketSession session);

        public event SessionEventDelegate Connected;

        public event SessionEventDelegate Disconnected;

        public event SessionEventDelegate SendData;

        public event SessionEventDelegate RecvData;

        private log4net.ILog log = Logger.GetInstance().GetLog();

        protected override void BeforeRecvData(SocketSession session, byte[] bytes, uint from, uint to)
        {
            log.Debug($"before recv data:{bytes}");
        }

        protected override void BeforeSendData(SocketSession session, byte[] bytes, uint from, uint to)
        {
            log.Debug($"before send data:{bytes}");
        }

        protected override void OnConnected(SocketSession session)
        {
            log.Info($"new client connected. socket = {session.BindSocket.Handle}");
            if (Connected != null) Connected(session);
        }

        protected override void OnDisConnected(SocketSession session)
        {
            log.Warn($" client disconnected. socket = {session.BindSocket.Handle}");
            if (Connected != null) Disconnected(session);
        }

        protected override void OnMainLoop()
        {
            serverSession.BindSocket.BeginAccept((IAsyncResult ia) =>
               {
                   Socket clientSocket = serverSession.BindSocket.EndAccept(ia);
                   AddClientSocket(clientSocket);
               }, null);

            if (clientSession.Count > 0)
            {
                var avalibleSocketList = clientSession.Keys;
                IList checkReadList = new List<Socket>(avalibleSocketList);
                IList checkWriteList = new List<Socket>(avalibleSocketList);
                IList checkErrorList = new List<Socket>(avalibleSocketList);

                Socket.Select(checkReadList, checkWriteList, checkErrorList, (int)SharpSvrSettings.GetInstance().SelectMicroSeconds);

                foreach (var socket in checkErrorList)
                {
                    RemoveClientSocket(socket as Socket);
                }

                readWriteSessionList.Clear();

                foreach (var socket in checkReadList)
                {
                    SocketSession session = null;
                    clientSession.TryGetValue(socket as Socket, out session);
                    if (session != null)
                    {
                        if (session.IsError)
                        {
                            RemoveClientSocket(session.BindSocket);
                            continue;
                        }
                        session.CanRead = true;
                        readWriteSessionList.Add(session);
                    }
                }

                foreach (var socket in checkWriteList)
                {
                    SocketSession session = null;
                    clientSession.TryGetValue(socket as Socket, out session);
                    if (session != null)
                    {
                        if (session.IsError)
                        {
                            RemoveClientSocket(session.BindSocket);
                            continue;
                        }
                        session.CanWrite = true;
                        readWriteSessionList.Add(session);
                    }
                }

                foreach (var session in readWriteSessionList)
                {
                    if (session.IsConnected) ThreadPool.QueueUserWorkItem(WaitCallback, session);
                    else RemoveClientSocket(session.BindSocket);
                }
            }

        }

        protected override void OnRecvData(SocketSession session, uint len)
        {
            log.Debug($"on recv data: socket={session.BindSocket.Handle}, len={len}");
            if (RecvData != null) RecvData(session);
        }

        protected override void OnSendData(SocketSession session, uint len)
        {
            log.Debug($"on send data: socket={session.BindSocket.Handle}, len={len}");
            if (SendData != null) SendData(session);
        }

        protected override void OnShutDown()
        {
            int tryCount = 0;

            serverSession.Close();

            var keys = clientSession.Keys;
            foreach (var socket in keys)
            {
                RemoveClientSocket(socket);
            }

            while (++tryCount <= SharpSvrSettings.GetInstance().ShutDownTryCount)
            {
                Thread.Sleep(1000);//这句写着，主要是没必要循环那么多次。去掉也可以。
                int maxWorkerThreads, workerThreads;
                int portThreads;
                ThreadPool.GetMaxThreads(out maxWorkerThreads, out portThreads);
                ThreadPool.GetAvailableThreads(out workerThreads, out portThreads);
                if (maxWorkerThreads - workerThreads == 0)
                {
                    log.Warn(" server shutdown success!");
                    break;
                }
            }
        }

        protected override void OnStartUp(string ip, short port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress iPAddress = null;
            iPAddress = Dns.GetHostEntry(ip).AddressList[0];
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, port);
            socket.Bind(iPEndPoint);
            socket.Listen((int)SharpSvrSettings.GetInstance().ListenBacklog);
            serverSession.WithSocket(socket);

        }
    }
}