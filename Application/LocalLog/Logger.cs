namespace LocalLog
{
    /// <summary>
    /// Centralized logging service for the grading system
    /// Handles both process logs and grade results
    /// </summary>
    public class Logger
    {
        private readonly string _logDirectory;
        private readonly object _lock = new object();
        
        public Logger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }
        
        /// <summary>
        /// Logs a message to the process log file
        /// </summary>
        public void LogProcess(string message, string level = "INFO")
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            
            lock (_lock)
            {
                File.AppendAllText(
                    Path.Combine(_logDirectory, "GradeProcess.log"),
                    logMessage + Environment.NewLine
                );
            }
            
            // Also write to console
            Console.WriteLine(logMessage);
        }
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        public void LogError(string message)
        {
            LogProcess(message, "ERROR");
        }
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        public void LogWarning(string message)
        {
            LogProcess(message, "WARN");
        }
        
        /// <summary>
        /// Logs a debug message
        /// </summary>
        public void LogDebug(string message)
        {
            LogProcess(message, "DEBUG");
        }
        
        /// <summary>
        /// Creates a summary text file
        /// </summary>
        public void SaveSummary(string content)
        {
            File.WriteAllText(
                Path.Combine(_logDirectory, "Summary.txt"),
                content
            );
        }
    }
}
