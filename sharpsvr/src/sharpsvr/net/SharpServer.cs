using System;
using System.Collections.Generic;
using System.Reflection;
using sharpsvr.attributes;
using sharpsvr.common;
using sharpsvr.protocol;
using sharpsvr.util;

namespace sharpsvr.net
{
    public class SharpServer : TcpServer
    {
        private Dictionary<string, Tuple<string, MethodInfo, object>> proxy;

        private log4net.ILog log = Logger.GetInstance().GetLog();

        public SharpServer(params object[] objects)
        {
            proxy = new Dictionary<string, Tuple<string, MethodInfo, object>>();
            WithService(objects);
        }

        public void ScanTypeProxy(Type type, object obj)
        {

            var serviceAttribute = AttributeUtils.GetRemoteServiceAttribute(type);
            if (serviceAttribute == null) return;
            var serviceName = serviceAttribute.Name;
            if (string.IsNullOrEmpty(serviceName)) serviceName = TypeUtils.GetTypeUniqueName(type);
            if (string.IsNullOrEmpty(serviceName))
            {
                log.Error($"type name for {type} not defined!");
                return;
            }

            log.Info($"scan service {serviceName} for type {type.FullName}");
            foreach (var methodInfo in type.GetMethods())
            {
                var methodName = TypeUtils.GetMethodUniqueName(methodInfo);
                if (string.IsNullOrEmpty(methodName))
                {
                    log.Error($"method name for {methodInfo} not defined!");
                    continue;
                }
                var fullName = TypeUtils.FullMethodName(serviceName, methodName);
                if (proxy.ContainsKey(fullName))
                {
                    log.Error($"found method proxy {fullName} more than once, ignored!");
                    continue;
                }
                proxy.Add(fullName, new Tuple<string, MethodInfo, object>(serviceAttribute.Version, methodInfo, obj));
                log.Info($"found method {fullName}.");
            }
            log.Info($"scan service {serviceName} for type {type.FullName} success!");

        }

        public SharpServer WithService(params object[] objects)
        {
            lock (proxy)
            {
                foreach (var obj in objects)
                {
                    if (obj == null) continue;
                    var type = obj.GetType();
                    var interfaces = type.GetInterfaces();
                    if (interfaces != null && interfaces.Length > 0)
                    {
                        foreach (var interfaceType in interfaces)
                        {
                            ScanTypeProxy(interfaceType, obj);
                        }
                    }
                    ScanTypeProxy(type, obj);
                }
            }
            return this;
        }

        protected override void OnStartUp(string ip, short port)
        {
            base.OnStartUp(ip, port);
            this.RecvData += this.TryDecodePacket;
        }

        private void TryDecodePacket(SocketSession session)
        {
            var packet = protocol.SharpProtocol.GetInstance().TryDecodePacket(session);
            if (packet != null)
            {
                ProcessClientPacket(session, packet);
            }
        }


        private void ProcessClientPacket(SocketSession session, SharpPacket packet)
        {
            log.Debug("request detected:" + packet);
            var fullName = TypeUtils.FullMethodName(packet.ServiceUniqueName, packet.MethodUniqueName);
            if (proxy.ContainsKey(fullName))
            {
                var methodInfo = proxy[fullName];
                if (!methodInfo.Item1.Equals(packet.Version))
                {
                    log.Warn($"packet version={packet.Version} and server version={methodInfo.Item1} not equals!");
                }
                try
                {
                    packet.Result = methodInfo.Item2.Invoke(methodInfo.Item3, ConvertUtils.ConvertObjectTypes(methodInfo.Item2, packet.Arguments));
                }
                catch (TargetInvocationException ex)
                {
                    log.Error("invoke method:" + packet.MethodUniqueName + " error!", ex);
                    packet.Result = new exception.WrapServerException(ex.InnerException.ToString());
                }catch(Exception ex){
                    log.Error("invoke method:" + packet.MethodUniqueName + " error!", ex);
                    packet.Result = new exception.WrapServerException(ex.Message.ToString());
                }
                var resultByteArray = protocol.SharpProtocol.GetInstance().Encode(packet);
                session.Send(protocol.SharpProtocol.GetHeader(resultByteArray));
                session.Send(resultByteArray);
            }
        }
    }
}