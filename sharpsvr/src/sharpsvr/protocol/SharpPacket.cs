using System;

namespace sharpsvr.protocol
{
    [Serializable]
    public class SharpPacket
    {
        public long Random{get;set;}

        public string ServiceUniqueName { get; set; }

        public string Version { get; set; } = ("1.0.0");

        public string MethodUniqueName { get; set; }

        public object[] Arguments { get; set; }

        public object Result { get; set; }

        public override string ToString(){
            return $"SharpPacket(ServiceUniqueName={ServiceUniqueName}, Version={Version}, MethodUniqueName={MethodUniqueName}, Result={Result})";
        }
    }
}