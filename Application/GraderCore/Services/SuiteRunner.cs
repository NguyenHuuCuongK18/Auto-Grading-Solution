using GraderCore.Abstractions;
using GraderCore.Models;

namespace GraderCore.Services
{
    /// <summary>
    /// Orchestrates the entire test suite execution
    /// Loads suite, executes test cases, generates reports
    /// </summary>
    public class SuiteRunner
    {
        private readonly ISuiteLoader _suiteLoader;
        private readonly ITestCaseParser _testCaseParser;
        private readonly ILoggingService _loggingService;
        
        public SuiteRunner(
            ISuiteLoader suiteLoader,
            ITestCaseParser testCaseParser,
            ILoggingService loggingService)
        {
            _suiteLoader = suiteLoader;
            _testCaseParser = testCaseParser;
            _loggingService = loggingService;
        }
        
        /// <summary>
        /// Executes the entire test suite
        /// </summary>
        public SuiteGradingResult ExecuteSuite(ExecuteSuiteArgs args)
        {
            var suiteResult = new SuiteGradingResult
            {
                StartTime = DateTime.Now
            };
            
            try
            {
                _loggingService.LogProcess("========================================");
                _loggingService.LogProcess("   AUTO-GRADING SYSTEM STARTED");
                _loggingService.LogProcess("========================================");
                _loggingService.LogProcess($"Suite Path: {args.SuitePath}");
                _loggingService.LogProcess($"Result Path: {args.ResultRoot}");
                
                // Load test suite configuration
                _loggingService.LogProcess("Loading test suite configuration...");
                var suite = _suiteLoader.LoadSuite(args.SuitePath);
                _loggingService.LogProcess($"Loaded {suite.TestCaseMarks.Count} test cases");
                
                // Prepare grading configuration
                var gradingConfig = args.GradingConfig ?? GradingConfig.Default;
                
                // Setup paths
                var clientWorkDir = string.IsNullOrEmpty(args.ClientExePath) 
                    ? args.SuitePath 
                    : Path.GetDirectoryName(args.ClientExePath) ?? args.SuitePath;
                    
                var serverWorkDir = string.IsNullOrEmpty(args.ServerExePath) 
                    ? args.SuitePath 
                    : Path.GetDirectoryName(args.ServerExePath) ?? args.SuitePath;
                
                // Determine database script path
                var databaseScript = args.DatabaseScriptPath;
                if (string.IsNullOrEmpty(databaseScript) && !string.IsNullOrEmpty(suite.Environment.DefaultDatabaseFilePath))
                {
                    databaseScript = Path.Combine(args.SuitePath, suite.Environment.DefaultDatabaseFilePath);
                }
                
                // Build connection string
                string? connectionString = null;
                if (!string.IsNullOrEmpty(suite.Environment.DefaultDatabaseName))
                {
                    connectionString = $"Server=localhost;Database={suite.Environment.DefaultDatabaseName};User Id={suite.Environment.DatabaseUsername};Password={suite.Environment.DatabasePassword};TrustServerCertificate=True;";
                    _loggingService.LogProcess($"Database: {suite.Environment.DefaultDatabaseName}");
                }
                
                // Create services for test execution
                var processManager = new ProcessManager();
                var middlewareService = new MiddlewareService(_loggingService);
                var comparisonService = new ComparisonService();
                var databaseService = new DatabaseService(_loggingService);
                
                var executor = new TestExecutor(
                    processManager,
                    middlewareService,
                    comparisonService,
                    databaseService,
                    _loggingService,
                    gradingConfig);
                
                // Execute each test case
                foreach (var (testCaseId, marks) in suite.TestCaseMarks)
                {
                    _loggingService.LogProcess($"\n>>> Executing Test Case: {testCaseId} ({marks} marks) <<<");
                    
                    try
                    {
                        // Parse test case
                        var testCasePath = Path.Combine(args.SuitePath, testCaseId);
                        if (!Directory.Exists(testCasePath))
                        {
                            _loggingService.LogProcess($"Test case folder not found: {testCasePath}", "ERROR");
                            suiteResult.CriticalErrors.Add($"Test case folder not found: {testCaseId}");
                            continue;
                        }
                        
                        var testCase = _testCaseParser.ParseTestCase(testCasePath, testCaseId, marks);
                        _loggingService.LogProcess($"Parsed {testCase.Stages.Count} stages");
                        
                        // Determine which executables to use based on GradeContent
                        string? clientExe = null;
                        string? serverExe = null;
                        
                        var gradeContent = testCase.Config.GradeContent.ToLowerInvariant();
                        
                        if (gradeContent.Contains("client") || gradeContent.Contains("both"))
                        {
                            clientExe = args.ClientExePath;
                        }
                        
                        if (gradeContent.Contains("server") || gradeContent.Contains("both"))
                        {
                            serverExe = args.ServerExePath;
                        }
                        
                        // Execute test case
                        var testCaseResult = executor.ExecuteTestCase(
                            testCase,
                            clientExe,
                            serverExe,
                            clientWorkDir,
                            serverWorkDir,
                            databaseScript,
                            connectionString);
                        
                        suiteResult.TestCaseResults.Add(testCaseResult);
                        suiteResult.TotalMaxMarks += testCaseResult.MaxMarks;
                        suiteResult.TotalEarnedMarks += testCaseResult.EarnedMarks;
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogProcess($"Test case {testCaseId} failed with exception: {ex.Message}", "ERROR");
                        suiteResult.CriticalErrors.Add($"Test case {testCaseId}: {ex.Message}");
                        
                        // Add failed test case result
                        suiteResult.TestCaseResults.Add(new TestCaseResult
                        {
                            TestCaseId = testCaseId,
                            MaxMarks = marks,
                            EarnedMarks = 0,
                            Passed = false,
                            Summary = $"Exception: {ex.Message}"
                        });
                        
                        suiteResult.TotalMaxMarks += marks;
                    }
                }
                
                suiteResult.EndTime = DateTime.Now;
                
                // Generate reports
                _loggingService.LogProcess("\n========================================");
                _loggingService.LogProcess("   GRADING COMPLETED");
                _loggingService.LogProcess("========================================");
                _loggingService.LogProcess($"Total Score: {suiteResult.TotalEarnedMarks:F2} / {suiteResult.TotalMaxMarks:F2} ({suiteResult.PercentageScore:F2}%)");
                _loggingService.LogProcess($"Duration: {(suiteResult.EndTime - suiteResult.StartTime).TotalSeconds:F2} seconds");
                
                _loggingService.LogProcess("\nGenerating reports...");
                _loggingService.GenerateReports(suiteResult, args.ResultRoot);
                
                _loggingService.LogProcess("========================================");
                _loggingService.LogProcess($"Results saved to: {args.ResultRoot}");
                _loggingService.LogProcess("========================================");
            }
            catch (Exception ex)
            {
                _loggingService.LogProcess($"CRITICAL ERROR: {ex.Message}", "ERROR");
                _loggingService.LogProcess($"Stack Trace: {ex.StackTrace}", "ERROR");
                suiteResult.CriticalErrors.Add($"Suite execution failed: {ex.Message}");
                suiteResult.EndTime = DateTime.Now;
            }
            
            return suiteResult;
        }
    }
}
