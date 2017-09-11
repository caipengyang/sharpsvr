using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using sharpsvr.attributes;

namespace sharpsvr.util
{
    public class TypeUtils
    {
        public static ConcurrentDictionary<Type, string> TypeUniqueNameMap = new ConcurrentDictionary<Type, string>();

        public static ConcurrentDictionary<MethodInfo, string> MethodUniqueNameMap = new ConcurrentDictionary<MethodInfo, string>();

        public static string GetTypeUniqueName(Type type)
        {
            if (TypeUniqueNameMap.ContainsKey(type)) return TypeUniqueNameMap[type];

            var attribute = AttributeUtils.GetRemoteServiceAttribute(type);
            if (attribute == null)
            {
                TypeUniqueNameMap[type] = type.Name;
            }
            else
            {
                var serviceName = attribute.Name;
                TypeUniqueNameMap[type] = string.IsNullOrEmpty(serviceName) ? type.Name : serviceName;
            }
            return TypeUniqueNameMap[type];
        }

        public static string GetMethodUniqueName(MethodInfo methodInfo)
        {
            if (MethodUniqueNameMap.ContainsKey(methodInfo)) return MethodUniqueNameMap[methodInfo];

            var attribute = AttributeUtils.GetRemoteMethodAttribute(methodInfo);

            if (attribute == null)
            {
                MethodUniqueNameMap[methodInfo] = methodInfo.Name;
            }
            else
            {
                var names = new List<string>();
                names.Add(string.IsNullOrEmpty(attribute.Name) ? methodInfo.Name : attribute.Name);
                foreach (var type in methodInfo.GetGenericArguments())
                {
                    names.Add(type.Name);
                }
                foreach (var param in methodInfo.GetParameters())
                {
                    names.Add(param.ParameterType.Name);
                }
                var result = string.Join('$', names);
                MethodUniqueNameMap[methodInfo] = result;
            }
            return MethodUniqueNameMap[methodInfo];
        }

        public static string FullMethodName(string serviceName, string methodName)
        {
            return $"{serviceName}.{methodName}";
        }
    }
}