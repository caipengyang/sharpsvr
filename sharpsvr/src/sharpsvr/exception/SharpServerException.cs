using System;
using System.Runtime.Serialization;

namespace sharpsvr.exception
{
    [Serializable]
    public class SharpServerException : SystemException
    {
        public SharpServerException() : base(){}

        public SharpServerException(string message) : base(message){}

        public SharpServerException(string message, Exception ex) : base(message, ex){}

        protected SharpServerException(SerializationInfo info, StreamingContext context):base(info, context){}
    }
}