using GraderCore.Models;

namespace GraderCore.Abstractions
{
    /// <summary>
    /// Service for loading test suite configuration from header.xlsx and environment.xlsx
    /// </summary>
    public interface ISuiteLoader
    {
        /// <summary>
        /// Loads test suite configuration from the given suite path
        /// </summary>
        /// <param name="suitePath">Path to the suite folder containing header.xlsx and environment.xlsx</param>
        /// <returns>Loaded test suite configuration</returns>
        TestSuite LoadSuite(string suitePath);
    }
    
    /// <summary>
    /// Service for parsing test case details from test case folder
    /// </summary>
    public interface ITestCaseParser
    {
        /// <summary>
        /// Parses a test case from its folder
        /// </summary>
        /// <param name="testCasePath">Path to test case folder</param>
        /// <param name="testCaseId">Test case ID</param>
        /// <param name="marks">Marks for this test case</param>
        /// <returns>Parsed test case with stages and expected data</returns>
        TestCase ParseTestCase(string testCasePath, string testCaseId, double marks);
    }
    
    /// <summary>
    /// Service for managing executable processes (client, server, middleware)
    /// </summary>
    public interface IProcessManager
    {
        /// <summary>
        /// Starts a process
        /// </summary>
        /// <param name="processName">Name/ID for the process (Client, Server, Middleware)</param>
        /// <param name="exePath">Path to executable</param>
        /// <param name="workingDirectory">Working directory</param>
        /// <param name="arguments">Command line arguments</param>
        /// <returns>True if started successfully</returns>
        bool StartProcess(string processName, string exePath, string workingDirectory, string? arguments = null);
        
        /// <summary>
        /// Sends input to a running process
        /// </summary>
        /// <param name="processName">Name of the process</param>
        /// <param name="input">Input text to send</param>
        void SendInput(string processName, string input);
        
        /// <summary>
        /// Gets console output from a process since last read
        /// </summary>
        /// <param name="processName">Name of the process</param>
        /// <returns>Console output text</returns>
        string GetOutput(string processName);
        
        /// <summary>
        /// Stops a process
        /// </summary>
        /// <param name="processName">Name of the process to stop</param>
        void StopProcess(string processName);
        
        /// <summary>
        /// Checks if a process is running
        /// </summary>
        /// <param name="processName">Name of the process</param>
        /// <returns>True if running</returns>
        bool IsProcessRunning(string processName);
        
        /// <summary>
        /// Stops all managed processes
        /// </summary>
        void StopAllProcesses();
    }
    
    /// <summary>
    /// Service for middleware proxy to capture network traffic
    /// </summary>
    public interface IMiddlewareService
    {
        /// <summary>
        /// Starts the middleware proxy
        /// </summary>
        /// <param name="clientPort">Port where client will connect</param>
        /// <param name="serverPort">Port where server is listening</param>
        /// <returns>True if started successfully</returns>
        bool StartMiddleware(int clientPort, int serverPort);
        
        /// <summary>
        /// Stops the middleware
        /// </summary>
        void StopMiddleware();
        
        /// <summary>
        /// Gets captured network traffic for a specific stage
        /// </summary>
        /// <param name="stageNumber">Stage number</param>
        /// <returns>Network data or null if none captured</returns>
        NetworkActual? GetNetworkDataForStage(int stageNumber);
        
        /// <summary>
        /// Clears captured network data
        /// </summary>
        void ClearNetworkData();
    }
    
    /// <summary>
    /// Service for database operations
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Resets database by executing the SQL script
        /// Injects ALTER DROP if needed
        /// </summary>
        /// <param name="scriptPath">Path to SQL script file</param>
        /// <param name="connectionString">Database connection string</param>
        /// <returns>True if successful</returns>
        bool ResetDatabase(string scriptPath, string connectionString);
    }
    
    /// <summary>
    /// Service for comparing expected vs actual data
    /// </summary>
    public interface IComparisonService
    {
        /// <summary>
        /// Compares two text strings (console output, JSON, etc.)
        /// </summary>
        /// <param name="expected">Expected text</param>
        /// <param name="actual">Actual text</param>
        /// <param name="normalize">Whether to normalize whitespace/formatting</param>
        /// <returns>Comparison result with match status and differences</returns>
        ComparisonResult Compare(string expected, string actual, bool normalize = true);
    }
    
    /// <summary>
    /// Result of a comparison operation
    /// </summary>
    public class ComparisonResult
    {
        /// <summary>
        /// Whether the values matched
        /// </summary>
        public bool Matched { get; set; }
        
        /// <summary>
        /// List of differences found
        /// </summary>
        public List<string> Differences { get; set; } = new();
        
        /// <summary>
        /// Excerpt highlighting the key difference
        /// </summary>
        public string? DifferenceExcerpt { get; set; }
    }
    
    /// <summary>
    /// Service for logging grading process and results
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Logs a message to the process log
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="level">Log level (INFO, WARN, ERROR)</param>
        void LogProcess(string message, string level = "INFO");
        
        /// <summary>
        /// Saves stage result for later reporting
        /// </summary>
        /// <param name="testCaseId">Test case ID</param>
        /// <param name="stageResult">Stage result to save</param>
        void LogStageResult(string testCaseId, StageResult stageResult);
        
        /// <summary>
        /// Generates final result reports (Excel and log files)
        /// </summary>
        /// <param name="suiteResult">Overall suite results</param>
        /// <param name="outputPath">Path where reports should be saved</param>
        void GenerateReports(SuiteGradingResult suiteResult, string outputPath);
    }
}
