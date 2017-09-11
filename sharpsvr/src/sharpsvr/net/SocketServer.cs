using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using sharpsvr.common;

namespace sharpsvr.net
{
    public abstract class SocketServer : IServer
    {
        private static log4net.ILog log = Logger.GetInstance().GetLog();

        protected SocketSession serverSession;

        protected ConcurrentDictionary<Socket, SocketSession> clientSession;

        private volatile bool isRunning;

        protected HashSet<SocketSession> readWriteSessionList;

        protected abstract void OnConnected(SocketSession session);

        protected abstract void OnDisConnected(SocketSession session);

        protected abstract void OnSendData(SocketSession session, uint len);

        protected abstract void OnRecvData(SocketSession session, uint len);

        protected abstract void BeforeSendData(SocketSession session, byte[] bytes, uint from, uint to);

        protected abstract void BeforeRecvData(SocketSession session, byte[] bytes, uint from, uint to);

        protected abstract void OnStartUp(string ip, short port);

        protected abstract void OnMainLoop();

        protected abstract void OnShutDown();

        public SocketServer()
        {
            clientSession = new ConcurrentDictionary<Socket, SocketSession>();
            serverSession = new ServerSocketSession();
            readWriteSessionList = new HashSet<SocketSession>();
        }

        public void StartUp(string ip = null, short port = 0)
        {
            if (string.IsNullOrEmpty(ip)) ip = SharpSvrSettings.GetInstance().BindIp;
            if (port <= 0) port = SharpSvrSettings.GetInstance().ListenPort;
            OnStartUp(ip, port);
            isRunning = true;
        }

        public void ShutDown()
        {
            log.Warn("going to shut down socket server...");
            isRunning = false;
            OnShutDown();
            isRunning = false;
            log.Warn("socket server shutdown success!");
        }

        public void MainLoop()
        {
            long runLoop = 0;
            while (isRunning)
            {
                if (runLoop++ % 10000 == 0)
                {
                    log.Debug("socket server is running..., active clients:" + clientSession.Count);
                }
                OnMainLoop();
                if (SharpSvrSettings.GetInstance().ServerMainSleepTime > 0) Thread.Sleep(SharpSvrSettings.GetInstance().ServerMainSleepTime);
            }
        }

        protected void AddClientSocket(Socket clientSocket)
        {
            if (clientSocket == null) return;
            if (!clientSession.ContainsKey(clientSocket))
            {
                var session = new ClientSocketSession().WithSocket(clientSocket);
                clientSession[clientSocket] = session;
                OnConnected(session);
                session.OnSend += (ref byte[] bytes, ref uint from, ref uint to) =>
                {
                    BeforeSendData(session, bytes, from, to);
                };
                session.OnRecv += (ref byte[] bytes, ref uint from, ref uint to) =>
                {
                    BeforeRecvData(session, bytes, from, to);
                };
            }
        }

        protected void RemoveClientSocket(Socket clientSocket)
        {
            if (clientSocket == null) return;
            SocketSession session = null;
            int maxRetryCount = 10;
            bool result = false;
            while(maxRetryCount-- > 0 && !result){
                result = clientSession.Remove(clientSocket, out session);
                if (session != null) OnDisConnected(session);
            }
        }

        public void WaitCallback(object state)
        {
            SocketSession session = state as SocketSession;
            if (session == null) return;
            lock (session)
            {
                var len = session.DoSend();
                if (len > 0)
                {
                    OnSendData(session, (uint)len);
                }
                len = session.DoRecv();
                if (len > 0)
                {
                    OnRecvData(session, (uint)len);
                }
            }
        }
    }
}