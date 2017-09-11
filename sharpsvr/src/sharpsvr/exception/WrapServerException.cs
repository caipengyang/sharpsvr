using System;

namespace sharpsvr.exception
{
    [Serializable]
    public class WrapServerException
    {
        public string Message{get;set;}

        public WrapServerException(string message)
        {
            Message = message;
        }

        public override string ToString(){
            return Message;
        }

    }
}