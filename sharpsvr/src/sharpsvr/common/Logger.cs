using System;
using System.IO;
using log4net;
using log4net.Config;
using log4net.Repository;

namespace sharpsvr.common
{
    public static class LogHelper{

        private enum LogLevelEnum
        {
            Debug, Info, Warn, Error, Fatal
        }

        private static void Log(this ILog log, LogLevelEnum levelEnum, string format, params object[] args)
        {
            switch (levelEnum)
            {
                case LogLevelEnum.Debug:
                    if (log.IsDebugEnabled)
                    {
                        string message = string.Format(format, args);
                        log.Debug(message);
                    }
                    break;
                case LogLevelEnum.Info:
                    if (log.IsInfoEnabled)
                    {
                        string message = string.Format(format, args);
                        log.Info(message);
                    }
                    break;
                case LogLevelEnum.Warn:
                    if (log.IsWarnEnabled)
                    {
                        string message = string.Format(format, args);
                        log.Warn(message);
                    }
                    break;
                case LogLevelEnum.Error:
                    if (log.IsErrorEnabled)
                    {
                        string message = string.Format(format, args);
                        log.Error(message);
                    }
                    break;
                case LogLevelEnum.Fatal:
                    if (log.IsFatalEnabled)
                    {
                        string message = string.Format(format, args);
                        log.Fatal(message);
                    }
                    break;
            }
        }
        public static void LogDebug(this ILog log, string format, params object[] args){
            Log(log, LogLevelEnum.Debug, format, args);
        }
        public static void LogInfo(this ILog log, string format, params object[] args){
            Log(log, LogLevelEnum.Info, format, args);
        }
        public static void LogWarn(this ILog log, string format, params object[] args){
            Log(log, LogLevelEnum.Warn, format, args);
        }
        public static void LogError(this ILog log, string format, params object[] args){
            Log(log, LogLevelEnum.Error, format, args);
        }

        public static void LogFatal(this ILog log, string format, params object[] args){
            Log(log, LogLevelEnum.Fatal, format, args);
        }

    }

    public class Logger : Singleton<Logger>
    {

        private ILoggerRepository respository = LogManager.CreateRepository("sharpsvr");

        public Logger()
        {
            log4net.Config.XmlConfigurator.Configure(respository, new FileInfo("app.config"));
        }

        public ILog GetLog(string name)
        {
            return LogManager.GetLogger(respository.Name, name);
        }

        public ILog GetLog(Type type)
        {
            return LogManager.GetLogger(respository.Name, type);
        }

        public ILog GetLog()
        {
            return GetLog("sharpsvr");
        }

    }
}