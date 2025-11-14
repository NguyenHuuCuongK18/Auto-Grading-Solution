using LocalLog;
using LocalDatabase;
using LocalGrade;
using LocalGraderConfig.Models;
using SingleStudentGrade.ExcelParsers;
using GraderCore.Services; // Temporary - Phase 2 will remove this (only SuiteRunner remains)
using GraderCore.Abstractions;

namespace SingleStudentGrade
{
    /// <summary>
    /// Main orchestrator for grading a single student's submission
    /// Uses ProcessLauncher pattern - delegates to specialized services
    /// 
    /// PHASE 1: Architecture established, delegates to existing GraderCore services
    /// PHASE 2: Migrating services to modular projects (SuiteLoader ✅, TestCaseParser ✅)
    /// </summary>
    public class GradingOrchestrator
    {
        private readonly Logger _logger;
        private readonly ILoggingService _graderCoreLogging;
        private readonly ExcelParsers.SuiteLoader _suiteLoader;  // MIGRATED from GraderCore
        private readonly ExcelParsers.TestCaseParser _testCaseParser;  // MIGRATED from GraderCore
        private readonly SuiteRunner _runner;
        
        public GradingOrchestrator(string outputDirectory)
        {
            // NEW: LocalLog for our orchestration logging
            _logger = new Logger(outputDirectory);
            _logger.LogProcess("=== Grading Orchestrator Initialized ===");
            _logger.LogProcess("Architecture: Modular (Phase 2 In Progress)");
            _logger.LogProcess("Pattern: ProcessLauncher delegation");
            
            // MIGRATED: Using modular Excel parsers from SingleStudentGrade
            _suiteLoader = new ExcelParsers.SuiteLoader();
            _testCaseParser = new ExcelParsers.TestCaseParser();
            
            // TEMPORARY: Still using GraderCore for logging and test execution
            _graderCoreLogging = new LoggingService();
            
            // Bridge pattern: Convert our parsers to GraderCore interfaces for SuiteRunner
            // This will be removed when we migrate SuiteRunner to LocalGrade
            ISuiteLoader graderCoreSuiteLoader = new SuiteLoaderBridge(_suiteLoader);
            ITestCaseParser graderCoreTestCaseParser = new TestCaseParserBridge(_testCaseParser);
            
            _runner = new SuiteRunner(graderCoreSuiteLoader, graderCoreTestCaseParser, _graderCoreLogging);
            
            _logger.LogProcess("Services initialized:");
            _logger.LogProcess("  - SuiteLoader: SingleStudentGrade.ExcelParsers ✅");
            _logger.LogProcess("  - TestCaseParser: SingleStudentGrade.ExcelParsers ✅");
            _logger.LogProcess("  - SuiteRunner: GraderCore (Phase 2 migration pending)");
        }
        
        /// <summary>
        /// Bridge adapter to convert our SuiteLoader to GraderCore ISuiteLoader interface
        /// Temporary - will be removed when SuiteRunner is migrated to LocalGrade
        /// </summary>
        private class SuiteLoaderBridge : ISuiteLoader
        {
            private readonly ExcelParsers.SuiteLoader _suiteLoader;
            
            public SuiteLoaderBridge(ExcelParsers.SuiteLoader suiteLoader)
            {
                _suiteLoader = suiteLoader;
            }
            
            public GraderCore.Models.TestSuite LoadSuite(string suitePath)
            {
                var suite = _suiteLoader.LoadSuite(suitePath);
                
                // Convert our TestSuite to GraderCore.Models.TestSuite
                // Models are identical, just in different namespaces
                return new GraderCore.Models.TestSuite
                {
                    SuitePath = suite.SuitePath,
                    TestCaseMarks = new Dictionary<string, double>(suite.TestCaseMarks),
                    Environment = new GraderCore.Models.EnvironmentConfig
                    {
                        EnvironmentType = suite.Environment.EnvironmentType,
                        AppType = suite.Environment.AppType,
                        CodeContainerInternalPort = suite.Environment.CodeContainerInternalPort,
                        CodeContainerHostPort = suite.Environment.CodeContainerHostPort,
                        DatabaseUsername = suite.Environment.DatabaseUsername,
                        DatabasePassword = suite.Environment.DatabasePassword,
                        DefaultDatabaseName = suite.Environment.DefaultDatabaseName,
                        DefaultDatabaseFilePath = suite.Environment.DefaultDatabaseFilePath,
                        RuntimesFolder = suite.Environment.RuntimesFolder,
                        GivenFolder = suite.Environment.GivenFolder,
                        AllConfig = new Dictionary<string, string>(suite.Environment.AllConfig)
                    }
                };
            }
        }
        
        /// <summary>
        /// Bridge adapter to convert our TestCaseParser to GraderCore ITestCaseParser interface
        /// Temporary - will be removed when SuiteRunner is migrated to LocalGrade
        /// </summary>
        private class TestCaseParserBridge : ITestCaseParser
        {
            private readonly ExcelParsers.TestCaseParser _parser;
            
            public TestCaseParserBridge(ExcelParsers.TestCaseParser parser)
            {
                _parser = parser;
            }
            
            public GraderCore.Models.TestCase ParseTestCase(string testCasePath, string testCaseId, double marks)
            {
                var testCase = _parser.ParseTestCase(testCasePath, testCaseId, marks);
                
                // Convert our TestCase to GraderCore.Models.TestCase
                var graderTestCase = new GraderCore.Models.TestCase
                {
                    TestCaseId = testCase.TestCaseId,
                    TestCasePath = testCase.TestCasePath,
                    Marks = testCase.Marks,
                    Config = new GraderCore.Models.TestCaseConfig
                    {
                        TimeoutSeconds = testCase.Config.TimeoutSeconds,
                        Domain = testCase.Config.Domain,
                        GradeContent = testCase.Config.GradeContent
                    }
                };
                
                // Convert stages
                foreach (var stage in testCase.Stages)
                {
                    var graderStage = new GraderCore.Models.TestStage
                    {
                        StageNumber = stage.StageNumber,
                        UserAction = new GraderCore.Models.UserAction
                        {
                            Action = stage.UserAction.Action,
                            Input = stage.UserAction.Input
                        },
                        ExpectedClientConsole = stage.ExpectedClientConsole,
                        ExpectedServerConsole = stage.ExpectedServerConsole
                    };
                    
                    if (stage.ExpectedNetwork != null)
                    {
                        graderStage.ExpectedNetwork = new GraderCore.Models.NetworkExpectation
                        {
                            Url = stage.ExpectedNetwork.Url,
                            RequestPayload = stage.ExpectedNetwork.RequestPayload,
                            ResponsePayload = stage.ExpectedNetwork.ResponsePayload
                        };
                    }
                    
                    graderTestCase.Stages.Add(graderStage);
                }
                
                return graderTestCase;
            }
        }
        
        /// <summary>
        /// Executes grading for a student submission
        /// This is the main entry point that orchestrates the entire grading workflow
        /// Uses ProcessLauncher pattern: delegates to specialized services
        /// </summary>
        public SuiteGradingResult ExecuteGrading(ExecuteSuiteArgs args)
        {
            _logger.LogProcess("=== Starting Grading Execution ===");
            _logger.LogProcess($"Suite: {args.SuitePath}");
            
            try
            {
                // Delegate to GraderCore (Phase 1)
                // Phase 2 will replace with modular service calls
                _logger.LogProcess("Delegating to GraderCore services...");
                
                // Both use same ExecuteSuiteArgs structure - can pass directly
                var graderArgs = new GraderCore.Models.ExecuteSuiteArgs
                {
                    SuitePath = args.SuitePath,
                    ResultRoot = args.ResultRoot,
                    ClientExePath = args.ClientExePath,
                    ServerExePath = args.ServerExePath,
                    ClientAppSettingsTemplate = args.ClientAppSettingsTemplate,
                    ServerAppSettingsTemplate = args.ServerAppSettingsTemplate,
                    DatabaseScriptPath = args.DatabaseScriptPath,
                    GradingConfig = args.GradingConfig != null 
                        ? new GraderCore.Models.GradingConfig 
                        {
                            ValidateClientConsole = args.GradingConfig.ValidateClientConsole,
                            ValidateServerConsole = args.GradingConfig.ValidateServerConsole,
                            ValidateNetworkTraffic = args.GradingConfig.ValidateNetworkTraffic,
                            StageTimeoutSeconds = args.GradingConfig.StageTimeoutSeconds
                        }
                        : new GraderCore.Models.GradingConfig()
                };
                
                var graderResult = _runner.ExecuteSuite(graderArgs);
                
                // Convert GraderCore result to our new result format
                var result = new SuiteGradingResult
                {
                    SuiteName = Path.GetFileName(args.SuitePath),
                    Success = graderResult.CriticalErrors.Count == 0,
                    TotalMarks = graderResult.TotalMaxMarks,
                    EarnedMarks = graderResult.TotalEarnedMarks,
                    StartTime = graderResult.StartTime,
                    EndTime = graderResult.EndTime,
                    CriticalErrors = graderResult.CriticalErrors
                };
                
                _logger.LogProcess($"Grading complete: {result.EarnedMarks:F2}/{result.TotalMarks:F2} marks");
                _logger.LogProcess("=== Orchestration Complete ===");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Orchestration failed: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                
                return new SuiteGradingResult
                {
                    SuiteName = Path.GetFileName(args.SuitePath),
                    Success = false,
                    ErrorMessage = ex.Message,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now
                };
            }
        }
    }
}
