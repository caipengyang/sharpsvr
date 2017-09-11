using System;
using System.IO;
using log4net;
using sharpsvr.common;
using sharpsvr.core;
using Xunit;

namespace sharpsvr.test.src.test.test.common
{
    public class TestByteBuffer
    {
        private static ILog log = Logger.GetInstance().GetLog();

        [Fact]
        public void TestWriteReadByteBuffer(){

            Console.WriteLine(Directory.GetCurrentDirectory());
            Func<String> RandomString = ()=>{
                String str = "";
                var random = new Random();
                int max = random.Next(10000);
                for(int i = 0; i < max; ++i){
                    str += (char)('a' + random.Next(26));
                }
                return str;
                };

            var byteBuffer = new ByteBuffer(1);
            Console.WriteLine( String.Format("byte buffer = {0}", byteBuffer));
            for(int i = 0; i < 6; ++i){
                Console.Write("{0:C3} :");
                string str = RandomString();
                var len = byteBuffer.WriteString(str);
                Console.WriteLine( String.Format("[[[ byte buffer = {0}", byteBuffer));

                String readStr;
                byteBuffer.ReadString(out readStr);
                Console.WriteLine( String.Format("byte buffer = {0}]]]\n", byteBuffer));
                Assert.Equal(str, readStr);
            }

            UInt16 uint16;
            byteBuffer.WriteUInt16(0xffff);
            byteBuffer.ReadUInt16(out uint16);
            Assert.Equal(uint16, 0xffff);

            UInt32 uint32;
            byteBuffer.WriteUInt32(0xffffffff);
            byteBuffer.ReadUInt32(out uint32);
            Assert.Equal(uint32, 0xffffffff);

            UInt64 uint64;
            byteBuffer.WriteUInt64(0x1234567812345678u);
            byteBuffer.ReadUInt64(out uint64);
            Assert.Equal(uint64, 0x1234567812345678u);

            byteBuffer.WriteUInt64(0x1234567812345678u);
            byteBuffer.ReadUInt16(out uint16);
            Assert.Equal(uint16, 0x5678);
            byteBuffer.ReadUInt32(out uint32);
            Assert.Equal(uint32, 0x56781234u);
        }
    }
}