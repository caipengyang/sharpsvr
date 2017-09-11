using System;
using log4net;
using sharpsvr.common;

namespace sharpsvr.core
{
    using UInt = UInt32;

    [Serializable]
    public class ByteBuffer : ICloneable
    {
        private static ILog log = Logger.GetInstance().GetLog();
        private byte[] buf;


        ///<summary>the start position of readable buffer.</summary>
        public UInt ReadPosition { get; set; } = 0;

        ///<summary>the size of readable buffer.</summary>
        public UInt Used { get; set; } = 0;

        public UInt Free { get { return (UInt)buf.Length - Used; } }

        public UInt WritePosition { get { return (UInt)((ReadPosition + Used) % buf.Length); } }

        public ByteBuffer(UInt bytes = 0)
        {
            buf = ByteArrayFactory.GetInstance().allocate(bytes > 0 ? bytes : common.SharpSvrSettings.GetInstance().ByteBufferDefaultSize);
            log.Debug(String.Format("new bytebuffer created. size={0}", buf.Length));
        }

        public void EnsureEnoughFreeSize(UInt size)
        {
            if (Free >= size) return;
            UInt newSize = (UInt)(buf.Length << 1);
            while (newSize - Used < size) newSize = newSize << 1;
            byte[] newBuf = ByteArrayFactory.GetInstance().allocate(newSize);
            log.LogDebug("allocate buffer size: from {0} to {1}", buf.Length, newSize);
            if (ReadPosition < WritePosition)
            {
                Array.Copy(buf, ReadPosition, newBuf, 0, Used);
            }
            else if (Used > 0)
            {
                Array.Copy(buf, ReadPosition, newBuf, 0, buf.Length - ReadPosition);
                Array.Copy(buf, 0, newBuf, buf.Length - ReadPosition, WritePosition);
            }
            ByteArrayFactory.GetInstance().recycle(buf);
            buf = newBuf;
            ReadPosition = 0;
        }

        public UInt WriteBytes(byte[] bytes, UInt from = 0, UInt to = 0)
        {
            if (bytes == null || from > to) return 0;
            if (to > bytes.Length || to == 0) to = (UInt)bytes.Length;
            UInt copySize = to - from;
            if (copySize == 0) return 0;
            EnsureEnoughFreeSize(copySize);
            if (buf.Length - ReadPosition - Used >= copySize || WritePosition < ReadPosition)
            {
                Array.Copy(bytes, from, buf, WritePosition, copySize);
            }
            else
            {
                UInt firstCopySize = (UInt)(buf.Length - WritePosition);
                Array.Copy(bytes, from, buf, WritePosition, firstCopySize);
                Array.Copy(bytes, from + firstCopySize, buf, 0, copySize - firstCopySize);
            }
            Used += copySize;
            return copySize;
        }

        public UInt ReadBytes(ref byte[] bytes, UInt len = 0)
        {
            if (bytes != null && len == 0) len = (UInt)bytes.Length;
            if (bytes == null)
            {
                if (len == 0) len = Used;
                bytes = ByteArrayFactory.GetInstance().allocate(len);
            }
            len = (UInt)Math.Min(Math.Min(len, bytes.Length), Used);
            if (ReadPosition + len <= buf.Length)
            {
                Array.Copy(buf, ReadPosition, bytes, 0, len);
            }
            else
            {
                UInt firstCopySize = (UInt)(buf.Length - ReadPosition);
                Array.Copy(buf, ReadPosition, bytes, 0, firstCopySize);
                Array.Copy(buf, 0, bytes, buf.Length - ReadPosition, len - firstCopySize);
            }
            Used -= len;
            ReadPosition = (UInt)((ReadPosition + len) % buf.Length);
            return len;
        }

        public UInt PeekBytes(ref byte[] bytes, UInt len = 0)
        {
            if (bytes != null && len == 0) len = (UInt)bytes.Length;
            if (bytes == null)
            {
                if (len == 0) len = Used;
                bytes = ByteArrayFactory.GetInstance().allocate(len);
            }
            len = (UInt)Math.Min(Math.Min(len, bytes.Length), Used);
            if (ReadPosition + len <= buf.Length)
            {
                Array.Copy(buf, ReadPosition, bytes, 0, len);
            }
            else
            {
                UInt firstCopySize = (UInt)(buf.Length - ReadPosition);
                Array.Copy(buf, ReadPosition, bytes, 0, firstCopySize);
                Array.Copy(buf, 0, bytes, buf.Length - ReadPosition, len - firstCopySize);
            }
            return len;
        }

        public UInt WriteString(String str)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            if (bytes.Length > UInt16.MaxValue - 2)
            {
                log.Error($"str too long to write! Len={bytes.Length}");
                return 0;
            }
            var sizeLen = WriteUInt16((UInt16)bytes.Length);
            return sizeLen + WriteBytes(bytes);
        }

        public UInt ReadString(out String str)
        {
            UInt16 size;
            var sizeLen = ReadUInt16(out size);
            if (sizeLen != 2)
            {
                log.Error($"string read failed! length size={sizeLen}");
                str = null;
                return 0;
            }
            byte[] buffer = null;
            sizeLen += ReadBytes(ref buffer, size);
            str = System.Text.Encoding.UTF8.GetString(buffer);
            ByteArrayFactory.GetInstance().recycle(buffer);
            return sizeLen;
        }

        public UInt WriteUInt16(UInt16 value)
        {

            byte[] buffer = { (byte)(value & 0xff), (byte)(value >> 8) };
            return WriteBytes(buffer);
        }

        public UInt ReadUInt16(out UInt16 value)
        {
            byte[] buffer = null;
            if (Used >= 2)
            {
                UInt len = ReadBytes(ref buffer, 2);
                value = (UInt16)((buffer[1] << 8) + buffer[0]);
                return len;
            }
            else
            {
                value = 0;
                return 0;
            }
        }


        public UInt WriteUInt32(UInt32 value)
        {

            byte[] buffer = { (byte)(value & 0xff), (byte)((value >> 8) & 0xff), (byte)((value >> 16) & 0xff), (byte)((value >> 24) & 0xff) };
            return WriteBytes(buffer);
        }

        public UInt ReadUInt32(out UInt32 value)
        {
            byte[] buffer = null;
            if (Used >= 4)
            {
                UInt len = ReadBytes(ref buffer, 4);
                value = (UInt32)((UInt32)(buffer[3] << 24) + (UInt32)(buffer[2] << 16) + (UInt32)(buffer[1] << 8) + (UInt32)buffer[0]);
                return len;
            }
            else
            {
                value = 0;
                return 0;
            }
        }

        public UInt WriteUInt64(UInt64 value)
        {
            byte[] buffer = {
                (byte)(value & 0xff),
                (byte) ((value >> 8) & 0xff),
                (byte) ((value >> 16) & 0xff),
                (byte) ((value >> 24) & 0xff),
                (byte) ((value >> 32) & 0xff),
                (byte) ((value >> 40) & 0xff),
                (byte) ((value >> 48) & 0xff),
                (byte) ((value >> 56) & 0xff)
                };
            return WriteBytes(buffer);
        }

        public UInt ReadUInt64(out UInt64 value)
        {
            byte[] buffer = null;
            if (Used >= 8)
            {
                UInt len = ReadBytes(ref buffer, 8);
                value = (((UInt64)buffer[3]) << 24) + ((UInt64)buffer[2] << 16) + ((UInt64)buffer[1] << 8) + buffer[0] +
                 (((UInt64)buffer[7]) << 56) + ((UInt64)buffer[6] << 48) + ((UInt64)buffer[5] << 40) + ((UInt64)buffer[4] << 32);
                return len;
            }
            else
            {
                value = 0;
                return 0;
            }
        }

        public byte this[int index] { get { return buf[(ReadPosition + index) % buf.Length]; } }


        public object Clone()
        {
            ByteBuffer byteBuffer = new ByteBuffer((UInt)this.buf.Length);
            byteBuffer.ReadPosition = this.ReadPosition;
            byteBuffer.Used = this.Used;
            Array.Copy(this.buf, byteBuffer.buf, this.buf.Length);
            return byteBuffer;
        }

        public override String ToString()
        {
            return $"Buf.Length={buf.Length}, ReadPosition={ReadPosition}, Used={Used}";
        }

    }
}