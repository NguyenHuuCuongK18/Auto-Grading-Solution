using log4net.Appender;
using log4net.Core;
using LogMaster.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogMaster
{
    public class BaseAppender : AppenderSkeleton
    {
        public string AppenderName { get; set; }
        public LevelAdapter.Log4NetLevel ThresholdLevel { get; set; } = LevelAdapter.Log4NetLevel.All;
        public IAppender Appender { get; set; }
        public string Layout { get; set; } = "%date{dd-MMM-yyyy-HH:mm:ss} [%class] [%level] [%method] - %message%newline";

        public BaseAppender(IAppender appender)
        {
            this.Appender = appender;
        }

        public BaseAppender()
        {
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            Appender.Append(RenderLoggingEvent(loggingEvent));
        }
    }
}
