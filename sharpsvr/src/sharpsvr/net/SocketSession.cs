using System;
using System.Net;
using System.Net.Sockets;
using sharpsvr.common;
using sharpsvr.core;

namespace sharpsvr.net
{
    public abstract class SocketSession
    {

        public delegate void ByteArrayTransactionDelegate(ref byte[] bytes, ref uint from, ref uint to);

        private static log4net.ILog log = Logger.GetInstance().GetLog();

        public Socket BindSocket { get; private set; }

        public ByteBuffer SendBuffer { get; private set; }

        public ByteBuffer RecvBuffer { get; private set; }

        private byte[] sendRecvPerTime = ByteArrayFactory.GetInstance().allocate(SharpSvrSettings.GetInstance().SendRecvPerTime);

        public bool IsConnected { get { return BindSocket != null ? BindSocket.Connected : false; } }

        public volatile bool IsError;

        public event ByteArrayTransactionDelegate OnSend;

        public event ByteArrayTransactionDelegate OnRecv;

        public bool CanRead { get; set; }

        public bool CanWrite { get; set; }

        public SocketSession()
        {
            SendBuffer = new ByteBuffer(SharpSvrSettings.GetInstance().SessionSendBufferSize);
            RecvBuffer = new ByteBuffer(SharpSvrSettings.GetInstance().SessionRecvBufferSize);
        }

        public SocketSession WithSocket(Socket socket)
        {
            this.BindSocket = socket;
            //socket.SetSocketOption(SocketOptionLevel.IP, )
            return this;
        }

        public int DoSend()
        {
            if (IsConnected && SendBuffer.Used > 0 && CanWrite)
            {
                var len = SendBuffer.ReadBytes(ref sendRecvPerTime, (uint)sendRecvPerTime.Length);
                uint from = 0, to = len;
                if (OnSend != null) OnSend(ref sendRecvPerTime, ref from, ref to);
                int send = 0;
                len = to - from;
                while (send < len) send += BindSocket.Send(sendRecvPerTime, (int)from + send, (int)(len - send), SocketFlags.None);
                CanWrite = false;
                return send;
            }
            return 0;
        }

        public int DoRecv()
        {
            try
            {
                if (IsConnected && CanRead)
                {
                    var len = BindSocket.Receive(sendRecvPerTime, 0, sendRecvPerTime.Length, SocketFlags.None);
                    uint from = 0, to = (uint)len;
                    if (OnRecv != null) OnRecv(ref sendRecvPerTime, ref from, ref to);
                    len = (int)(to - from);
                    if (len > 0) RecvBuffer.WriteBytes(sendRecvPerTime, from, to);
                    else{
                        log.Debug($"connection disconnected! socket={BindSocket.Handle}");
                        IsError = true;
                    }
                    CanRead = false;
                    return len;
                }
                return 0;
            }
            catch (Exception ex)
            {
                if(IsConnected) return 0;
                log.Error("DoRecv error!", ex);
                IsError = true;
                return -1;
            }
        }

        public int Send(byte[] bytes, uint from = 0, uint to = 0)
        {
            try
            {
                if (bytes == null) return 0;
                if (to == 0) to = (uint)bytes.Length;
                if (from >= to) return 0;
                if (IsConnected)
                {
                    SendBuffer.WriteBytes(bytes, from, to);
                    return (int)(to - from);
                }
                else
                {
                    log.Error("socket is not connected! Data send failed!");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                log.Error("DoSend error!", ex);
                IsError = true;
                return -1;
            }
        }

        public uint DoSendTo(EndPoint endPoint, byte[] bytes, uint from = 0, uint to = 0){
            var len = BindSocket.SendTo(bytes,(int) from, (int)(to - from), SocketFlags.None, endPoint);
            return 0;
        }

        public uint BroadCast(byte[] bytes, uint from=0, uint to=0){
            return 0;
        }

        public uint DoRecvFrom(EndPoint endPoint){
            uint from = 0, to =(uint) sendRecvPerTime.Length;
            var len = BindSocket.ReceiveFrom(sendRecvPerTime, (int)from, (int)to, SocketFlags.None, ref endPoint);
            if(len > 0){
                to = (uint)(from + len);
                if(OnRecv != null){
                    OnRecv(ref sendRecvPerTime, ref from, ref to);
                }
                len = (int)(to - from);
                if( len > 0){
                    RecvBuffer.WriteBytes(sendRecvPerTime, from, to);
                }
            }
            return 0;
        }

        public void Close(){
            if(IsConnected){
                try{
                    BindSocket.Close();
                }finally{
                }
            }
        }

    }
}