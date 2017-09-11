using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using Microsoft.Extensions.Configuration;
using sharpsvr.util;

namespace sharpsvr.common
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    class BindPropertyAttribute : Attribute
    {
        public string name;

        public object value;
    }

    public class ServerConfig
    {
        public string ServerName { get; set; }

        public string ServerIp { get; set; }

        public short ServerPort { get; set; } = 10086;

        public byte Weight { get; set; } = 1;

        public override string ToString()
        {
            return $"ServerName={ServerName}, ServerIp={ServerIp}, ServerPort={ServerPort}, Weight={Weight}";
        }

    }

    public class ServiceConfig
    {

        public string ServiceName { get; set; }

        public string ServiceVersion { get; set; }

        public string TypeName { get; set; }

        public List<ServerConfig> ServerList = new List<ServerConfig>();

        public override String ToString()
        {
            return $"ServiceName={ServiceName}, ServiceVersion={ServiceVersion}, TypeName={TypeName}, serverList={string.Join(',', ServerList)}";
        }

    }

    public class SharpSvrSettings : Singleton<SharpSvrSettings>
    {

        private static ILog log = Logger.GetInstance().GetLog();

        public SharpSvrSettings()
        {
            IConfigurationRoot configuration = null;
            if (File.Exists("app.config"))
            {
                log.Info("use app.config file to set properties.");
                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddXmlFile("app.config");
                configuration = builder.Build();
            }
            else
            {
                log.Warn("app.config file not found! use default settings.");
            }
            Type type = GetType();
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                var attribute = AttributeUtils.GetCustomAttribute<BindPropertyAttribute>(property);
                if (attribute != null)
                {
                    Object value = configuration != null ? configuration[attribute.name] : null;
                    value = Convert.ChangeType(value ?? attribute.value, property.PropertyType);
                    property.SetValue(this, value ?? attribute.value);
                }
            }

            Name2AllService = new Dictionary<string, ServiceConfig>();
            Type2AllService = new Dictionary<Type, ServiceConfig>();
            var remoteServiceSection = configuration.GetSection("remote-services");
            if (remoteServiceSection != null)
            {
                var serviceSescions = remoteServiceSection.GetChildren();
                foreach (var middleSection in serviceSescions)
                {
                    foreach (var serviceSection in middleSection.GetChildren())
                    {
                        var serviceConfig = new ServiceConfig
                        {
                            ServiceName = serviceSection.Key,
                            ServiceVersion = string.IsNullOrEmpty(serviceSection["version"]) ? "1.0.0" : serviceSection["version"],
                            TypeName = serviceSection["type"]
                        };
                        var serverSection = serviceSection.GetSection("servers");
                        if (serverSection == null) continue;
                        foreach (var subMiddleSection in serverSection.GetChildren())
                        {
                            foreach (var childSection in subMiddleSection.GetChildren())
                            {
                                serviceConfig.ServerList.Add(new ServerConfig
                                {
                                    ServerName = childSection.Key,
                                    ServerIp = childSection["ip"],
                                    ServerPort = short.Parse(childSection["port"]),
                                    Weight = (byte)(string.IsNullOrEmpty(childSection["weight"]) ? 1 : byte.Parse(childSection["weight"]))
                                });
                            }
                        }
                        if (serviceConfig.ServiceName != null)
                        {
                            log.Warn("found service:" + serviceConfig);
                            Name2AllService.Add(serviceConfig.ServiceName, serviceConfig);
                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                if (assembly.GetType(serviceConfig.TypeName) != null)
                                {
                                    Type2AllService.Add(assembly.GetType(serviceConfig.TypeName), serviceConfig);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        [BindProperty(name = "bytebuffer.size", value = 1024)]
        public uint ByteBufferDefaultSize { get; private set; }

        [BindProperty(name = "session.sendbuffer.size", value = 1024)]
        public uint SessionSendBufferSize { get; private set; }

        [BindProperty(name = "session.recvbuffer.size", value = 1024)]
        public uint SessionRecvBufferSize { get; private set; }

        [BindProperty(name = "session.sendrecv.per.time", value = 1024)]
        public uint SendRecvPerTime { get; private set; }

        [BindProperty(name = "server.bind.ip", value = "127.0.0.1")]
        public string BindIp { get; private set; }

        [BindProperty(name = "client.connect.ip", value = "127.0.0.1")]
        public string ConnectIp { get; private set; }

        [BindProperty(name = "server.listen.port", value = 10086)]
        public short ListenPort { get; private set; }

        [BindProperty(name = "client.connect.port", value = 10086)]
        public short ConnectPort { get; private set; }

        [BindProperty(name = "client.recv.timeout", value = 1000000)]
        public int ClientRecvTimeout { get; private set; }

        [BindProperty(name = "client.sendrecv.time.unit", value = 100)]
        public int ClientSendRecvTimeUnit { get; private set; }

        [BindProperty(name = "server.listen.backlog", value = 128)]
        public uint ListenBacklog { get; private set; }

        [BindProperty(name = "server.select.microsecond", value = 100)]
        public uint SelectMicroSeconds { get; private set; }

        [BindProperty(name = "server.shutdown.try.count", value = 10)]
        public uint ShutDownTryCount { get; private set; }

        [BindProperty(name = "server.udp.recv.timeout", value = -1)]
        public int UdpRecvTimeOut { get; private set; }

        [BindProperty(name = "sharpsvr.serialize.type", value = "json")]
        public string SerializerType { get; private set; }

        [BindProperty(name = "server.main.sleep.time", value = 1)]
        public int ServerMainSleepTime { get; private set; }

        [BindProperty(name = "client.keepalive", value = true)]
        public bool ClientKeepAlive { get; private set; } = true;

        [BindProperty(name = "client.max.connect.fail.count", value = 10)]
        public int MaxConnectFailCount{get; private set;}

        public Dictionary<String, ServiceConfig> Name2AllService { get; private set; }

        public Dictionary<Type, ServiceConfig> Type2AllService { get; private set; }

    }
}
