using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;

namespace LogMaster.Interfaces
{
    public class LevelAdapter
    {
        public static Level GetLogLevel(Log4NetLevel level)
        {
            Level logLevel;

            switch (level)
            {
                case Log4NetLevel.All:
                    logLevel = Level.All;
                    break;
                case Log4NetLevel.DEBUG:
                    logLevel = Level.Debug;
                    break;
                case Log4NetLevel.INFO:
                    logLevel = Level.Info;
                    break;
                case Log4NetLevel.WARN:
                    logLevel = Level.Warn;
                    break;
                case Log4NetLevel.ERROR:
                    logLevel = Level.Error;
                    break;
                case Log4NetLevel.FATAL:
                    logLevel = Level.Fatal;
                    break;
                default:
                    logLevel = Level.All;
                    break;
            }
            return logLevel;
        }

        public enum Log4NetLevel
        {
            All, DEBUG, INFO, WARN, ERROR, FATAL
        }
    }
}
