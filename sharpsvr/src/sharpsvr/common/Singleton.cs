namespace sharpsvr.common
{
    public class Singleton<T> where T : new()
    {
        private static T instance;

        public static T GetInstance()
        {
            if (instance != null) return instance;
            lock (typeof(Singleton<T>))
            {
                if (instance == null)
                {
                    instance = new T();
                }
            }
            return instance;
        }
    }
}