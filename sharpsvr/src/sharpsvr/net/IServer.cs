namespace sharpsvr.net
{
    public interface IServer
    {
        void StartUp(string ip=null, short port=0);

        void MainLoop();

        void ShutDown();
    }
}