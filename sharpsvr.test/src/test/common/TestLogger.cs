using sharpsvr.common;
using Xunit;

namespace sharpsvr.src.test.common
{
    public class TestLogger  
    {
        [Fact]
        public void TestGetLogger(){
            Assert.NotNull(Logger.GetInstance());
        }
        
    }
}