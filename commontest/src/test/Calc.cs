using System.Collections.Generic;

namespace commontest.src.test
{
    public class Calc : ICalc
    {
        public double Add(int a, long b, float c = 1, double d = 2)
        {
            return a + b + c + d;
        }

        public User GetUser(User a = null, User b = null)
        {
            System.Console.WriteLine($"a={a}, b={b}");
            throw new System.NotImplementedException();
        }

        public List<User> GetUserList(User user = null)
        {
            var result = new List<User>();
            result.Add(user);
            result.Add(new User{Id=1, Age=2, Sex=true, Message = "hello,hahah", Child=user});
            return result;
        }

        public void SayHello(string name = "yangyang")
        {
            System.Console.WriteLine("hello:" + name);
        }

        public int TestA(short b)
        {
            return b++;
        }

        public int TestA(int c, long d)
        {
            return (int)(c * d);
        }
    }
}
