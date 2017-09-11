using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace sharpsvr.util
{
    public class ConvertUtils
    {

        public static object[] ConvertObjectTypes(MethodInfo methodInfo, object[] arguments)
        {
            if (arguments == null || arguments.Length == 0) return arguments;
            int index = -1;
            object[] copy = null;
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                index++;
                if (arguments[index] == null) continue;
                if (parameterInfo.ParameterType == arguments[index].GetType()) continue;
                if (copy == null)
                {
                    copy = new object[arguments.Length];
                    Array.Copy(arguments, copy, copy.Length);
                }
                copy[index] = ToSrcObject(arguments[index], parameterInfo.ParameterType);
            }
            return copy == null ? arguments : copy;
        }

        public static object ToSrcObject(object obj, Type type){
            if(obj == null) return obj;
            var serializer = protocol.SerializerManager.GetInstance().GetSerializer();
            //common.Logger.GetInstance().GetLog().Debug("objec type:" + obj.GetType());
            if(serializer is protocol.serializer.JsonSerializer && obj.GetType().IsSubclassOf(typeof(Newtonsoft.Json.Linq.JContainer))){
                return JsonConvert.DeserializeObject((obj as Newtonsoft.Json.Linq.JContainer).ToString(), type);
            }
            return Convert.ChangeType(obj, type);
        }

        public static object ConvertResultType(MethodInfo methodInfo, object result){
            if(result == null) return result;
            if(result.GetType() == methodInfo.ReturnType || result.GetType().IsSubclassOf(methodInfo.ReturnType)) return result;
            var changeResult = ToSrcObject(result, methodInfo.ReturnType);
            return changeResult == null ? result : changeResult;
        }
    }
}
