using System.Collections.Generic;
using sharpsvr.attributes;

namespace commontest.src.test
{
    [RemoteService]
    public interface ICalc
    {
        [RemoteMethod]
        void SayHello(string name="yangyang");

        [RemoteMethod]
        int TestA(short b);

        [RemoteMethod]
        int TestA(int c, long d);

        [RemoteMethod]
        double Add(int a, long b, float c=1.0f, double d = 2.0);

        [RemoteMethod]
        List<User> GetUserList(User user = null);

        [RemoteMethod]
        User GetUser(User a=null, User b=null);
         
    }
}