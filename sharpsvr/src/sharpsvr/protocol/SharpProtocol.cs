using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using sharpsvr.common;
using sharpsvr.core;
using sharpsvr.net;
using sharpsvr.util;

namespace sharpsvr.protocol
{
    public class SharpProtocol : Singleton<SharpProtocol>
    {
        public const string SVR = "svr0";

        public const int HEADER_LEN = 8;

        private static ILog log = Logger.GetInstance().GetLog();

        private Random random = new Random();

        public static string GetVersion(Type type)
        {
            var attribute = AttributeUtils.GetRemoteServiceAttribute(type);
            return attribute == null ? "1.0.0" : attribute.Version;
        }

        public static byte[] GetHeader(byte[] content)
        {
            uint len = (uint)content.Length;
            return new byte[]{(byte)SVR[0], (byte)SVR[1], (byte)SVR[2], (byte)SVR[3], (byte)(len & 0xff), (byte)((len >> 8) & 0xff),
            (byte)((len >> 16) & 0xff), (byte)((len >> 24) & 0xff)};
        }


        public byte[] Encode(MethodInfo methodInfo, object[] args)
        {
            return Encode(new SharpPacket
                {
                    Version = GetVersion(methodInfo.DeclaringType),
                    ServiceUniqueName = TypeUtils.GetTypeUniqueName(methodInfo.DeclaringType),
                    MethodUniqueName = TypeUtils.GetMethodUniqueName(methodInfo),
                    Arguments = args,
                    Random = random.Next()
                }, false);
        }

        public byte[] Encode(SharpPacket packet, bool isServerSide=true){
            if(packet.Random == 0) packet.Random = random.Next();
            var serializer = SerializerManager.GetInstance().GetSerializer();
            //服务端向客户端发送,不必要数据置空,减少浏览
            if(isServerSide){
                packet.MethodUniqueName = packet.ServiceUniqueName = packet.Version = null;
                packet.Arguments = null;
            } 
            return serializer.Serialize(packet);
        }

        public SharpPacket TryDecodePacket(SocketSession socketSession){
            if(socketSession.RecvBuffer.Used < HEADER_LEN) return null;
            if(socketSession.RecvBuffer[0] != 's' || socketSession.RecvBuffer[1] != 'v' ||
            socketSession.RecvBuffer[2] != 'r' || socketSession.RecvBuffer[3] != '0'){
                log.Error("recv buffer error! protocol header check failed!");
                socketSession.Close();
                return null;
            }
            var fullPacketSize = (socketSession.RecvBuffer[4]) + (socketSession.RecvBuffer[5] << 8) +
            (socketSession.RecvBuffer[6] << 16) + (socketSession.RecvBuffer[7] << 24) + HEADER_LEN;
            if(fullPacketSize <= socketSession.RecvBuffer.Used){
                var buffer = ByteArrayFactory.GetInstance().allocate((uint)(fullPacketSize));
                socketSession.RecvBuffer.ReadBytes(ref buffer,(uint) fullPacketSize);
                var serializer = SerializerManager.GetInstance().GetSerializer();
                var obj = serializer.Deserialize<SharpPacket>(buffer, HEADER_LEN, fullPacketSize);
                log.Debug($"recv packet:{obj}, client session:{socketSession.BindSocket.Handle}");
                return obj as SharpPacket;
            }
            return null;
        }

    }
}