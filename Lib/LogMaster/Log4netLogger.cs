using log4net;
using log4net.Appender;
using log4net.Core;
using LogMaster.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;

namespace LogMaster
{
    public class Log4netLogger : ILogger
    {
        private static ILog _logger;
        private static ILogger Instance;
        private static readonly object _lock = new object();
        Action<BaseAppender> Action;


        public static ILogger GetLogger(Type type, string applicationName)
        {
            Log4NetManager.SetupLogger(applicationName);
            _logger = log4net.LogManager.GetLogger(type);
            if (Instance == null)
            {
                lock (_lock)
                {
                    Instance = new Log4netLogger();
                    return Instance;
                }
            }
            return Instance;
        }

        public void SetLayout(string layout)
        {
            Log4NetManager.Layout = layout;
        }

        public static void UseConsoleAppender()
        {
            Log4NetManager.AddAppender(Log4NetManager.GetConsoleAppender());
        }

        public static void UseFileAppender(string location = "Logs/FileLogger.log")
        {
            Log4NetManager.AddAppender(Log4NetManager.GetFileAppender(location));
        }

        public static void UseRollingFileAppender(string location = "Logs/RollingFile.log")
        {
            Log4NetManager.AddAppender(Log4NetManager.GetRollingFileAppender(location));
        }

        public static void UseCustomerAppender(IAppender Appender)
        {

            BaseAppender baseAppender = new BaseAppender(Appender);
            Log4NetManager.AddAppender(baseAppender);
        }
        public static void UseCustomerAppender(Action<BaseAppender> option)
        {
            BaseAppender baseAppender = new BaseAppender();
            Log4NetManager.AddAppender(baseAppender, option);
        }

        public void LogDebug(string message)
        {
            _logger.Debug(message);
        }

        public void LogDebug(string message, string tag)
        {
            _logger.Debug("[" + tag + "]: " + message);
        }

        public void LogErr(string message)
        {
            _logger.Error(message);
        }

        public void LogErr(string message, string tag)
        {
            _logger.Error("[" + tag + "]: " + message);
        }

        public void LogFatal(string message)
        {
            _logger.Fatal(message);
        }

        public void LogFatal(string message, string tag)
        {
            _logger.Fatal("[" + tag + "]: " + message);
        }

        public void LogInfo(string message)
        {
            _logger.Info(message);
        }

        public void LogInfo(string message, string tag)
        {
            _logger.Info("[" + tag + "]: " + message);
        }

        public void LogWarn(string message)
        {
            _logger.Warn(message);
        }

        public void LogWarn(string message, string tag)
        {
            _logger.Warn("[" + tag + "]: " + message);
        }


    }
}
