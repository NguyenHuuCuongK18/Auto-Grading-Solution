using LocalGraderConfig.Models;
using LocalGraderConfig.Keywords;
using ProcessLauncher;
using LocalLog;
using LocalDatabase;

namespace LocalGrade
{
    /// <summary>
    /// Main test execution engine following ProcessLauncher pattern
    /// Orchestrates test case execution, process management, data capture, and validation
    /// Migrated from GraderCore to LocalGrade project
    /// </summary>
    public class TestExecutor
    {
        private readonly Logger _logger;
        private readonly DatabaseService _databaseService;
        private readonly IComparisonService _comparisonService;
        private readonly GradingConfig _gradingConfig;
        
        // Process management using ProcessLauncher pattern
        private readonly Dictionary<string, ProcessInfo> _runningProcesses;
        private readonly Dictionary<string, string> _processOutputBuffers;
        private readonly object _processLock = new();
        
        /// <summary>
        /// Creates a new TestExecutor with specified dependencies
        /// </summary>
        public TestExecutor(
            Logger logger,
            DatabaseService databaseService,
            IComparisonService comparisonService,
            GradingConfig gradingConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
            _gradingConfig = gradingConfig ?? throw new ArgumentNullException(nameof(gradingConfig));
            
            _runningProcesses = new Dictionary<string, ProcessInfo>();
            _processOutputBuffers = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Executes a single test case through ProcessLauncher pattern
        /// </summary>
        public TestCaseResult ExecuteTestCase(
            TestCase testCase,
            string? clientExePath,
            string? serverExePath,
            string clientWorkDir,
            string serverWorkDir,
            string? databaseScript,
            string? connectionString)
        {
            _logger.LogProcess($"========== Starting Test Case: {testCase.TestCaseId} ==========");
            
            var result = new TestCaseResult
            {
                TestCaseId = testCase.TestCaseId,
                MaxMarks = testCase.Marks,
                Passed = true // Assume pass until proven otherwise
            };
            
            try
            {
                // Reset database if script provided
                if (!string.IsNullOrEmpty(databaseScript) && !string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogProcess("Resetting database...");
                    if (!_databaseService.ResetDatabase(databaseScript, connectionString))
                    {
                        // Database reset failed - log as warning but continue
                        // (SQL Server may not be available in all environments)
                        _logger.LogProcess("Database reset failed - continuing without database reset", "WARN");
                        result.Errors.Add("Database reset failed (non-critical)");
                    }
                }
                
                // Execute each stage using ProcessLauncher pattern
                foreach (var stage in testCase.Stages)
                {
                    _logger.LogProcess($"--- Stage {stage.StageNumber}: {stage.UserAction?.Action} ---");
                    
                    var stageResult = ExecuteStage(stage, testCase.Config, clientExePath, serverExePath, clientWorkDir, serverWorkDir);
                    result.StageResults.Add(stageResult);
                    
                    LogStageResult(testCase.TestCaseId, stageResult);
                    
                    if (!stageResult.Passed)
                    {
                        result.Passed = false;
                        _logger.LogProcess($"Stage {stage.StageNumber} FAILED", "WARN");
                    }
                    
                    // Wait a bit between stages for processes to settle
                    Thread.Sleep(500);
                }
                
                // Calculate marks based on passed stages
                var totalStages = result.StageResults.Count;
                var passedStages = result.StageResults.Count(s => s.Passed);
                
                if (totalStages > 0)
                {
                    result.EarnedMarks = (testCase.Marks * passedStages) / totalStages;
                }
                
                result.Summary = $"Passed {passedStages}/{totalStages} stages";
                
                _logger.LogProcess($"Test Case {testCase.TestCaseId} completed: {result.EarnedMarks:F2}/{testCase.Marks:F2} marks");
            }
            catch (Exception ex)
            {
                _logger.LogProcess($"Test case execution error: {ex.Message}", "ERROR");
                result.Errors.Add($"Execution error: {ex.Message}");
                result.Passed = false;
                result.EarnedMarks = 0;
            }
            finally
            {
                // Clean up all processes using ProcessLauncher pattern
                _logger.LogProcess("Cleaning up processes...");
                StopAllProcesses();
            }
            
            return result;
        }
        
        /// <summary>
        /// Executes a single stage using ProcessLauncher pattern
        /// </summary>
        private StageResult ExecuteStage(
            TestStage stage,
            TestCaseConfig config,
            string? clientExePath,
            string? serverExePath,
            string clientWorkDir,
            string serverWorkDir)
        {
            var stageResult = new StageResult
            {
                StageNumber = stage.StageNumber,
                Action = stage.UserAction?.Action ?? "Unknown",
                Passed = true,
                ExpectedClientConsole = stage.ExpectedClientConsole,
                ExpectedServerConsole = stage.ExpectedServerConsole,
                ExpectedNetwork = stage.ExpectedNetwork
            };
            
            try
            {
                // Perform action using ProcessLauncher pattern
                switch (stage.UserAction?.Action)
                {
                    case ProcessKeywords.Action_StartClient:
                        HandleStartClient(clientExePath, clientWorkDir, stageResult);
                        break;
                        
                    case ProcessKeywords.Action_StartServer:
                        HandleStartServer(serverExePath, serverWorkDir, stageResult);
                        break;
                        
                    case ProcessKeywords.Action_Input:
                        HandleInput(stage.UserAction.Input, stageResult);
                        break;
                        
                    case ProcessKeywords.Action_CloseClient:
                        HandleCloseClient(stageResult);
                        break;
                        
                    case ProcessKeywords.Action_CloseServer:
                        HandleCloseServer(stageResult);
                        break;
                }
                
                // Give processes time to generate output
                Thread.Sleep(1000);
                
                // Capture outputs using ProcessLauncher pattern
                lock (_processLock)
                {
                    if (_runningProcesses.ContainsKey(ProcessKeywords.ProcessName_Client))
                    {
                        stageResult.ActualClientConsole = GetProcessOutput(ProcessKeywords.ProcessName_Client);
                    }
                    
                    if (_runningProcesses.ContainsKey(ProcessKeywords.ProcessName_Server))
                    {
                        stageResult.ActualServerConsole = GetProcessOutput(ProcessKeywords.ProcessName_Server);
                    }
                }
                
                // Validate outputs against expectations
                ValidateStage(stageResult, config);
            }
            catch (Exception ex)
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add($"Stage execution error: {ex.Message}");
                _logger.LogProcess($"Stage {stage.StageNumber} error: {ex.Message}", "ERROR");
            }
            
            return stageResult;
        }
        
        /// <summary>
        /// Handles StartClient action using ProcessLauncher
        /// </summary>
        private void HandleStartClient(string? clientExePath, string clientWorkDir, StageResult stageResult)
        {
            if (string.IsNullOrEmpty(clientExePath))
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add("Client executable path not provided");
                return;
            }
            
            if (!File.Exists(clientExePath))
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add($"Client executable not found: {clientExePath}");
                return;
            }
            
            _logger.LogProcess($"Starting client: {clientExePath}");
            
            var started = StartProcessAsync(
                ProcessKeywords.ProcessName_Client,
                clientExePath,
                clientWorkDir);
            
            if (!started)
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add("Failed to start client process");
                _logger.LogProcess("CRITICAL: Failed to start client", "ERROR");
            }
        }
        
        /// <summary>
        /// Handles StartServer action using ProcessLauncher
        /// </summary>
        private void HandleStartServer(string? serverExePath, string serverWorkDir, StageResult stageResult)
        {
            if (string.IsNullOrEmpty(serverExePath))
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add("Server executable path not provided");
                return;
            }
            
            if (!File.Exists(serverExePath))
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add($"Server executable not found: {serverExePath}");
                return;
            }
            
            _logger.LogProcess($"Starting server: {serverExePath}");
            
            var started = StartProcessAsync(
                ProcessKeywords.ProcessName_Server,
                serverExePath,
                serverWorkDir);
            
            if (!started)
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add("Failed to start server process");
                _logger.LogProcess("CRITICAL: Failed to start server", "ERROR");
            }
            
            // Note: NetworkingMiddleware will be implemented later
            _logger.LogProcess("Network middleware not yet implemented");
        }
        
        /// <summary>
        /// Handles Input action
        /// </summary>
        private void HandleInput(string input, StageResult stageResult)
        {
            _logger.LogProcess($"Sending input to client: '{input}'");
            
            lock (_processLock)
            {
                if (!_runningProcesses.ContainsKey(ProcessKeywords.ProcessName_Client))
                {
                    stageResult.Passed = false;
                    stageResult.ValidationMessages.Add("Cannot send input: client not running");
                    return;
                }
                
                // Send input would be implemented via ProcessLauncher
                // For now, log that it's not fully implemented
                _logger.LogProcess("Input handling via ProcessLauncher to be fully implemented");
            }
        }
        
        /// <summary>
        /// Handles CloseClient action
        /// </summary>
        private void HandleCloseClient(StageResult stageResult)
        {
            _logger.LogProcess("Stopping client");
            StopProcess(ProcessKeywords.ProcessName_Client);
        }
        
        /// <summary>
        /// Handles CloseServer action
        /// </summary>
        private void HandleCloseServer(StageResult stageResult)
        {
            _logger.LogProcess("Stopping server");
            StopProcess(ProcessKeywords.ProcessName_Server);
        }
        
        /// <summary>
        /// Validates stage outputs against expectations using ComparisonService
        /// </summary>
        private void ValidateStage(StageResult stageResult, TestCaseConfig config)
        {
            // Validate client console if expected and config says to validate
            if (_gradingConfig.ValidateClientConsole &&
                !string.IsNullOrEmpty(stageResult.ExpectedClientConsole))
            {
                var comparison = _comparisonService.Compare(
                    stageResult.ExpectedClientConsole,
                    stageResult.ActualClientConsole ?? string.Empty);
                
                stageResult.ClientConsoleMatched = comparison.Matched;
                
                if (!comparison.Matched)
                {
                    stageResult.Passed = false;
                    stageResult.ValidationMessages.Add("Client console output mismatch");
                    stageResult.ValidationMessages.AddRange(comparison.Differences);
                }
            }
            
            // Validate server console if expected and config says to validate
            if (_gradingConfig.ValidateServerConsole &&
                !string.IsNullOrEmpty(stageResult.ExpectedServerConsole))
            {
                var comparison = _comparisonService.Compare(
                    stageResult.ExpectedServerConsole,
                    stageResult.ActualServerConsole ?? string.Empty);
                
                stageResult.ServerConsoleMatched = comparison.Matched;
                
                if (!comparison.Matched)
                {
                    stageResult.Passed = false;
                    stageResult.ValidationMessages.Add("Server console output mismatch");
                    stageResult.ValidationMessages.AddRange(comparison.Differences);
                }
            }
            
            // Network validation will be implemented with NetworkingMiddleware
            if (_gradingConfig.ValidateNetworkTraffic &&
                stageResult.ExpectedNetwork != null)
            {
                _logger.LogProcess("Network validation not yet implemented (NetworkingMiddleware pending)");
                stageResult.ValidationMessages.Add("Network validation not yet implemented");
            }
            
            if (stageResult.Passed)
            {
                _logger.LogProcess($"Stage {stageResult.StageNumber} validation: PASS");
            }
            else
            {
                _logger.LogProcess($"Stage {stageResult.StageNumber} validation: FAIL - {string.Join(", ", stageResult.ValidationMessages)}", "WARN");
            }
        }
        
        /// <summary>
        /// Starts a process using ProcessLauncher pattern
        /// </summary>
        private bool StartProcessAsync(string processName, string exePath, string workingDirectory)
        {
            lock (_processLock)
            {
                // Stop existing process with same name if any
                if (_runningProcesses.ContainsKey(processName))
                {
                    StopProcess(processName);
                }
                
                try
                {
                    // Start process asynchronously using ProcessLauncher
                    Task.Run(async () =>
                    {
                        try
                        {
                            var (stdout, stderr, exitCode) = await ProcessRunner.RunAsync(
                                exePath,
                                arguments: string.Empty,
                                workingDirectory: workingDirectory,
                                timeout: 30000); // 30 second timeout per process
                            
                            // Store output
                            lock (_processLock)
                            {
                                var output = stdout + stderr;
                                if (_processOutputBuffers.ContainsKey(processName))
                                {
                                    _processOutputBuffers[processName] += output;
                                }
                                else
                                {
                                    _processOutputBuffers[processName] = output;
                                }
                                
                                // Remove from running processes when complete
                                _runningProcesses.Remove(processName);
                            }
                            
                            _logger.LogProcess($"Process {processName} completed with exit code {exitCode}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogProcess($"Process {processName} error: {ex.Message}", "ERROR");
                            lock (_processLock)
                            {
                                _runningProcesses.Remove(processName);
                            }
                        }
                    });
                    
                    // Store process info
                    _runningProcesses[processName] = new ProcessInfo
                    {
                        Name = processName,
                        ExePath = exePath,
                        StartTime = DateTime.Now
                    };
                    
                    _processOutputBuffers[processName] = string.Empty;
                    
                    // Give process a moment to start
                    Thread.Sleep(500);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogProcess($"Failed to start process {processName}: {ex.Message}", "ERROR");
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Gets accumulated output from a process
        /// </summary>
        private string GetProcessOutput(string processName)
        {
            lock (_processLock)
            {
                if (_processOutputBuffers.TryGetValue(processName, out var output))
                {
                    return output;
                }
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Stops a specific process
        /// </summary>
        private void StopProcess(string processName)
        {
            lock (_processLock)
            {
                if (_runningProcesses.ContainsKey(processName))
                {
                    _logger.LogProcess($"Stopping process: {processName}");
                    _runningProcesses.Remove(processName);
                    // Process will complete or timeout on its own
                }
            }
        }
        
        /// <summary>
        /// Stops all running processes
        /// </summary>
        private void StopAllProcesses()
        {
            lock (_processLock)
            {
                foreach (var processName in _runningProcesses.Keys.ToList())
                {
                    StopProcess(processName);
                }
                _runningProcesses.Clear();
            }
        }
        
        /// <summary>
        /// Logs stage result details
        /// </summary>
        private void LogStageResult(string testCaseId, StageResult stageResult)
        {
            _logger.LogProcess($"Stage {stageResult.StageNumber} Result:");
            _logger.LogProcess($"  Action: {stageResult.Action}");
            _logger.LogProcess($"  Passed: {stageResult.Passed}");
            
            if (stageResult.ValidationMessages.Any())
            {
                _logger.LogProcess($"  Validation Messages:");
                foreach (var msg in stageResult.ValidationMessages)
                {
                    _logger.LogProcess($"    - {msg}");
                }
            }
        }
        
        /// <summary>
        /// Tracks information about a running process
        /// </summary>
        private class ProcessInfo
        {
            public string Name { get; set; } = string.Empty;
            public string ExePath { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
        }
    }
}
