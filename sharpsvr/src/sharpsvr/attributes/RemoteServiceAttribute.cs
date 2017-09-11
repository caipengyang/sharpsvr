using System;

namespace sharpsvr.attributes
{
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Class, AllowMultiple=false, Inherited = false)]
    public class RemoteServiceAttribute : Attribute
    {
        public string Name {get;set;}  = null;
        public string Version{get;set;} = "1.0.0";
    }
}