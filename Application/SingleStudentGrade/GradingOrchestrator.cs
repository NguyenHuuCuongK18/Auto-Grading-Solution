using LocalLog;
using LocalDatabase;
using LocalGrade;
using LocalGraderConfig.Models;
using GraderCore.Services; // Temporary - Phase 2 will remove this
using GraderCore.Abstractions;

namespace SingleStudentGrade
{
    /// <summary>
    /// Main orchestrator for grading a single student's submission
    /// Uses ProcessLauncher pattern - delegates to specialized services
    /// 
    /// PHASE 1: Architecture established, delegates to existing GraderCore services
    /// PHASE 2: Will migrate all services to modular projects
    /// </summary>
    public class GradingOrchestrator
    {
        private readonly Logger _logger;
        private readonly ILoggingService _graderCoreLogging;
        private readonly ISuiteLoader _suiteLoader;
        private readonly ITestCaseParser _testCaseParser;
        private readonly SuiteRunner _runner;
        
        public GradingOrchestrator(string outputDirectory)
        {
            // NEW: LocalLog for our orchestration logging
            _logger = new Logger(outputDirectory);
            _logger.LogProcess("=== Grading Orchestrator Initialized ===");
            _logger.LogProcess("Architecture: Modular (Phase 1 Complete)");
            _logger.LogProcess("Pattern: ProcessLauncher delegation");
            
            // TEMPORARY: Still using GraderCore services (Phase 2 will migrate)
            _graderCoreLogging = new LoggingService();
            _suiteLoader = new SuiteLoader();
            _testCaseParser = new TestCaseParser();
            _runner = new SuiteRunner(_suiteLoader, _testCaseParser, _graderCoreLogging);
            
            _logger.LogProcess("Services initialized (GraderCore bridge - Phase 2 migration pending)");
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
