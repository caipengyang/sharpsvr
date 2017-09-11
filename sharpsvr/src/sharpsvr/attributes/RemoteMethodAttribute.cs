using System;

namespace sharpsvr.attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
    public class RemoteMethodAttribute : Attribute
    {
        public string Name {get;set;} = null;
    }
}