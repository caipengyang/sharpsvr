using System;
using System.Reflection;
using sharpsvr.common;

namespace sharpsvr.proxy
{
    public abstract class Interceptor
    {
        private log4net.ILog log = Logger.GetInstance().GetLog();
        public virtual object Invoke(MethodInfo methodInfo, object[] args)
        {
            log.Info($"invoke called method={(methodInfo == null ? "null" : methodInfo.Name)}, args.len={(args==null ? 0 : args.Length)}");
            if(args != null){
                for(int i = 0; i < args.Length; ++i){
                    log.Info($"arg[{i}]={args[i]}");
                }
            }
            return methodInfo.ReturnType.IsValueType ? Activator.CreateInstance(methodInfo.ReturnType) : null;
        }
    }
}