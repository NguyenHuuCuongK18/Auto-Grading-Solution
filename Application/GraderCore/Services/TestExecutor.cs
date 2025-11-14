using GraderCore.Abstractions;
using GraderCore.Keywords;
using GraderCore.Models;

namespace GraderCore.Services
{
    /// <summary>
    /// Main test execution engine
    /// Orchestrates test case execution, process management, data capture, and validation
    /// </summary>
    public class TestExecutor
    {
        private readonly IProcessManager _processManager;
        private readonly IMiddlewareService _middlewareService;
        private readonly IComparisonService _comparisonService;
        private readonly IDatabaseService _databaseService;
        private readonly ILoggingService _loggingService;
        private readonly GradingConfig _gradingConfig;
        
        public TestExecutor(
            IProcessManager processManager,
            IMiddlewareService middlewareService,
            IComparisonService comparisonService,
            IDatabaseService databaseService,
            ILoggingService loggingService,
            GradingConfig gradingConfig)
        {
            _processManager = processManager;
            _middlewareService = middlewareService;
            _comparisonService = comparisonService;
            _databaseService = databaseService;
            _loggingService = loggingService;
            _gradingConfig = gradingConfig;
        }
        
        /// <summary>
        /// Executes a single test case
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
            _loggingService.LogProcess($"========== Starting Test Case: {testCase.TestCaseId} ==========");
            
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
                    _loggingService.LogProcess("Resetting database...");
                    if (!_databaseService.ResetDatabase(databaseScript, connectionString))
                    {
                        // Database reset failed - log as warning but continue
                        // (SQL Server may not be available in all environments)
                        _loggingService.LogProcess("Database reset failed - continuing without database reset", "WARN");
                        result.Errors.Add("Database reset failed (non-critical)");
                    }
                }
                
                // Execute each stage
                foreach (var stage in testCase.Stages)
                {
                    _loggingService.LogProcess($"--- Stage {stage.StageNumber}: {stage.UserAction?.Action} ---");
                    
                    var stageResult = ExecuteStage(stage, testCase.Config, clientExePath, serverExePath, clientWorkDir, serverWorkDir);
                    result.StageResults.Add(stageResult);
                    
                    _loggingService.LogStageResult(testCase.TestCaseId, stageResult);
                    
                    if (!stageResult.Passed)
                    {
                        result.Passed = false;
                        _loggingService.LogProcess($"Stage {stage.StageNumber} FAILED", "WARN");
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
                
                _loggingService.LogProcess($"Test Case {testCase.TestCaseId} completed: {result.EarnedMarks:F2}/{testCase.Marks:F2} marks");
            }
            catch (Exception ex)
            {
                _loggingService.LogProcess($"Test case execution error: {ex.Message}", "ERROR");
                result.Errors.Add($"Execution error: {ex.Message}");
                result.Passed = false;
                result.EarnedMarks = 0;
            }
            finally
            {
                // Clean up all processes
                _loggingService.LogProcess("Cleaning up processes...");
                _processManager.StopAllProcesses();
                _middlewareService.StopMiddleware();
            }
            
            return result;
        }
        
        /// <summary>
        /// Executes a single stage
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
                // Perform action
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
                
                // Capture outputs
                if (_processManager.IsProcessRunning(ProcessKeywords.ProcessName_Client))
                {
                    stageResult.ActualClientConsole = _processManager.GetOutput(ProcessKeywords.ProcessName_Client);
                }
                
                if (_processManager.IsProcessRunning(ProcessKeywords.ProcessName_Server))
                {
                    stageResult.ActualServerConsole = _processManager.GetOutput(ProcessKeywords.ProcessName_Server);
                }
                
                // Validate outputs against expectations
                ValidateStage(stageResult, config);
            }
            catch (Exception ex)
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add($"Stage execution error: {ex.Message}");
                _loggingService.LogProcess($"Stage {stage.StageNumber} error: {ex.Message}", "ERROR");
            }
            
            return stageResult;
        }
        
        /// <summary>
        /// Handles StartClient action
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
            
            _loggingService.LogProcess($"Starting client: {clientExePath}");
            
            var started = _processManager.StartProcess(
                ProcessKeywords.ProcessName_Client,
                clientExePath,
                clientWorkDir);
            
            if (!started)
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add("Failed to start client process");
                _loggingService.LogProcess("CRITICAL: Failed to start client", "ERROR");
            }
        }
        
        /// <summary>
        /// Handles StartServer action
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
            
            _loggingService.LogProcess($"Starting server: {serverExePath}");
            
            var started = _processManager.StartProcess(
                ProcessKeywords.ProcessName_Server,
                serverExePath,
                serverWorkDir);
            
            if (!started)
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add("Failed to start server process");
                _loggingService.LogProcess("CRITICAL: Failed to start server", "ERROR");
            }
            
            // If both client and server are now running, start middleware
            if (_processManager.IsProcessRunning(ProcessKeywords.ProcessName_Client) &&
                _processManager.IsProcessRunning(ProcessKeywords.ProcessName_Server))
            {
                _loggingService.LogProcess("Both client and server running - starting middleware");
                _middlewareService.StartMiddleware(8001, 8000);
            }
        }
        
        /// <summary>
        /// Handles Input action
        /// </summary>
        private void HandleInput(string input, StageResult stageResult)
        {
            _loggingService.LogProcess($"Sending input to client: '{input}'");
            
            if (!_processManager.IsProcessRunning(ProcessKeywords.ProcessName_Client))
            {
                stageResult.Passed = false;
                stageResult.ValidationMessages.Add("Cannot send input: client not running");
                return;
            }
            
            _processManager.SendInput(ProcessKeywords.ProcessName_Client, input);
        }
        
        /// <summary>
        /// Handles CloseClient action
        /// </summary>
        private void HandleCloseClient(StageResult stageResult)
        {
            _loggingService.LogProcess("Stopping client");
            _processManager.StopProcess(ProcessKeywords.ProcessName_Client);
            
            // Stop middleware if either client or server stops
            _middlewareService.StopMiddleware();
        }
        
        /// <summary>
        /// Handles CloseServer action
        /// </summary>
        private void HandleCloseServer(StageResult stageResult)
        {
            _loggingService.LogProcess("Stopping server");
            _processManager.StopProcess(ProcessKeywords.ProcessName_Server);
            
            // Stop middleware if either client or server stops
            _middlewareService.StopMiddleware();
        }
        
        /// <summary>
        /// Validates stage outputs against expectations
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
            
            // Validate network data if expected and config says to validate
            if (_gradingConfig.ValidateNetworkTraffic &&
                stageResult.ExpectedNetwork != null)
            {
                stageResult.ActualNetwork = _middlewareService.GetNetworkDataForStage(stageResult.StageNumber);
                
                if (stageResult.ActualNetwork == null)
                {
                    stageResult.Passed = false;
                    stageResult.NetworkMatched = false;
                    stageResult.ValidationMessages.Add("No network data captured (middleware not implemented)");
                }
                else
                {
                    // Validate network data
                    var matched = ValidateNetworkData(stageResult.ExpectedNetwork, stageResult.ActualNetwork, stageResult);
                    stageResult.NetworkMatched = matched;
                    
                    if (!matched)
                    {
                        stageResult.Passed = false;
                    }
                }
            }
            
            if (stageResult.Passed)
            {
                _loggingService.LogProcess($"Stage {stageResult.StageNumber} validation: PASS");
            }
            else
            {
                _loggingService.LogProcess($"Stage {stageResult.StageNumber} validation: FAIL - {string.Join(", ", stageResult.ValidationMessages)}", "WARN");
            }
        }
        
        /// <summary>
        /// Validates network data
        /// </summary>
        private bool ValidateNetworkData(NetworkExpectation expected, NetworkActual actual, StageResult stageResult)
        {
            bool allMatched = true;
            
            // Validate URL
            if (!string.IsNullOrEmpty(expected.Url))
            {
                var comparison = _comparisonService.Compare(expected.Url, actual.Url, normalize: false);
                if (!comparison.Matched)
                {
                    allMatched = false;
                    stageResult.ValidationMessages.Add($"URL mismatch: expected '{expected.Url}', got '{actual.Url}'");
                }
            }
            
            // Validate response payload
            if (!string.IsNullOrEmpty(expected.ResponsePayload))
            {
                var comparison = _comparisonService.Compare(expected.ResponsePayload, actual.ResponsePayload);
                if (!comparison.Matched)
                {
                    allMatched = false;
                    stageResult.ValidationMessages.Add("Response payload mismatch");
                    stageResult.ValidationMessages.AddRange(comparison.Differences);
                }
            }
            
            return allMatched;
        }
    }
}
