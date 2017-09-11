using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using sharpsvr.attributes;

namespace sharpsvr.util
{
    public class AttributeUtils
    {

        private static ConcurrentDictionary<Type, RemoteServiceAttribute> RemoteServiceAttributeMap = new ConcurrentDictionary<Type, RemoteServiceAttribute>();
        private static ConcurrentDictionary<MethodInfo, RemoteMethodAttribute> RemoteMethodAttributeMap = new ConcurrentDictionary<MethodInfo, RemoteMethodAttribute>();

        public static T GetCustomAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {

            var attributes = memberInfo.GetCustomAttributes(typeof(T), true);
            return attributes == null || attributes.Length <= 0 ? null : attributes[0] as T;
        }
        public static RemoteServiceAttribute GetRemoteServiceAttribute(Type type)
        {
            if (RemoteServiceAttributeMap.ContainsKey(type)) return RemoteServiceAttributeMap[type];
            RemoteServiceAttributeMap[type] = GetCustomAttribute<RemoteServiceAttribute>(type);
            return RemoteServiceAttributeMap[type];
        }

        public static RemoteMethodAttribute GetRemoteMethodAttribute(MethodInfo methodInfo)
        {
            if (RemoteMethodAttributeMap.ContainsKey(methodInfo)) return RemoteMethodAttributeMap[methodInfo];
            RemoteMethodAttributeMap[methodInfo] = GetCustomAttribute<RemoteMethodAttribute>(methodInfo);
            return RemoteMethodAttributeMap[methodInfo];
        }

    }
}