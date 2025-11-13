using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Appender;
using log4net.Layout;
using log4net.Config;
using LogMaster.Interfaces;

namespace LogMaster
{
    public class Log4NetManager
    {
        #region Field

        private static ILog _logger;
        private static ConsoleAppender _consoleAppender;
        private static FileAppender _fileAppender;
        private static RollingFileAppender _rollingFileAppender;
        private static string _layout = "%date{dd-MMM-yyyy-HH:mm:ss} [%level] - %message%newline";
        private static string applicationName;
        #endregion
        public static string Layout
        {
            set { _layout = value; }
        }

        public static void SetupLogger(string applicatonName)
        {
            //var hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
            //foreach (var appender in hierarchy.Root.Appenders)
            //{
            //    hierarchy.Root.RemoveAppender(appender);
            //}

            log4net.GlobalContext.Properties["logFileName"] = applicatonName;

        }


        public static void AddAppender(AppenderSkeleton appender)
        {
            //default
            var patterLayout = new PatternLayout()
            {
                ConversionPattern = _layout
            };
            patterLayout.ActivateOptions();
            appender.Layout = patterLayout;
            BasicConfigurator.Configure(appender);
        }

        public static void AddAppender(AppenderSkeleton appender, Action<BaseAppender> option)
        {

            BaseAppender baseAppender = (BaseAppender)appender;
            option(baseAppender);
            var patterLayout = new PatternLayout()
            {
                ConversionPattern = baseAppender.Layout
            };
            patterLayout.ActivateOptions();
            appender.Layout = patterLayout;
            appender.Threshold = LevelAdapter.GetLogLevel(baseAppender.ThresholdLevel);
            appender.Name = baseAppender.Name;
            BasicConfigurator.Configure(appender);
        }

        private static PatternLayout GetPatternLayout()
        {
            var patterLayout = new PatternLayout()
            {
                ConversionPattern = _layout
            };
            patterLayout.ActivateOptions();
            return patterLayout;
        }



        public static ConsoleAppender GetConsoleAppender()
        {
            var consoleAppender = new ConsoleAppender()
            {
                Name = "ConsoleAppender",
                Layout = GetPatternLayout(),
                Threshold = Level.Info
            };
            consoleAppender.ActivateOptions();
            return consoleAppender;
        }

        public static FileAppender GetFileAppender(string location)
        {

            var fileAppender = new FileAppender()
            {
                Name = "FileAppender",
                Layout = GetPatternLayout(),
                Threshold = Level.Debug,
                AppendToFile = true,
                File = location,
            };
            fileAppender.ActivateOptions();
            return fileAppender;
        }

        public static RollingFileAppender GetRollingFileAppender(string location)
        {
            var rollingAppender = new RollingFileAppender()
            {
                Name = "Rolling File Appender",
                AppendToFile = true,
                File = $"Logs/RollingFile_{applicationName}.log",
                Layout = GetPatternLayout(),
                Threshold = Level.All,
                MaximumFileSize = "1MB",
                MaxSizeRollBackups = 15 //file1.log,file2.log.....file15.log
            };
            rollingAppender.ActivateOptions();
            return rollingAppender;
        }



    }
}
