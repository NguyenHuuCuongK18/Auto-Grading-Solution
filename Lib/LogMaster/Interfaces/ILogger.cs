using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogMaster
{
    public interface ILogger
    {
        #region Setup
        //void SetLayout(string layout);


        //void UseConsoleAppender();


        //void UseFileAppender();

        //void UseRollingFileAppender();

        #endregion

        #region Fatal
        void LogFatal(string message);
        void LogFatal(string message, string tag);
        #endregion

        #region Info
        void LogInfo(string message);
        void LogInfo(string message, string tag);
        #endregion

        #region Error
        void LogErr(string message);
        void LogErr(string message, string tag);
        #endregion

        #region Warn
        void LogWarn(string message);
        void LogWarn(string message, string tag);
        #endregion

        #region Debug
        void LogDebug(string message);
        void LogDebug(string message, string tag);
        #endregion
    }
}
