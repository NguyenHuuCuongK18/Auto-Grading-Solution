using LocalGraderConfig.Models;
using GraderCore.Services; // Temporary - will be removed in Phase 2
using SingleStudentGrade;

namespace AutoGradingClient
{
    /// <summary>
    /// Command-line interface for the auto-grading system
    /// Usage: Client ExecuteSuite --suite <path> --out <path> [options]
    /// Uses modular architecture with ProcessLauncher pattern
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("===========================================");
                Console.WriteLine("   AUTO-GRADING SOLUTION");
                Console.WriteLine("   Local Windows Grading System");
                Console.WriteLine("===========================================");
                Console.WriteLine();
                
                if (args.Length == 0)
                {
                    return PrintUsage();
                }
                
                var verb = args[0].Trim().ToLowerInvariant();
                var argsMap = ParseArgs(args.Skip(1).ToArray());
                
                return verb switch
                {
                    "executesuite" => ExecuteSuite(argsMap),
                    "help" or "--help" or "-h" => PrintUsage(),
                    _ => PrintUsage()
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
                return -1;
            }
        }
        
        /// <summary>
        /// Executes a test suite
        /// </summary>
        private static int ExecuteSuite(Dictionary<string, string> args)
        {
            // Validate required arguments
            if (!args.ContainsKey("suite") || !args.ContainsKey("out"))
            {
                Console.Error.WriteLine("ERROR: --suite and --out are required arguments");
                return PrintUsage();
            }
            
            // Create timestamped results folder
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var timestampedResultRoot = Path.Combine(args["out"], $"GradeResult_{timestamp}");
            
            // Build execution arguments
            var executeArgs = new ExecuteSuiteArgs
            {
                SuitePath = args["suite"],
                ResultRoot = timestampedResultRoot,
                ClientExePath = args.GetValueOrDefault("client"),
                ServerExePath = args.GetValueOrDefault("server"),
                ClientAppSettingsTemplate = args.GetValueOrDefault("client-appsettings"),
                ServerAppSettingsTemplate = args.GetValueOrDefault("server-appsettings"),
                DatabaseScriptPath = args.GetValueOrDefault("db-script")
            };
            
            // Parse grading config
            if (args.TryGetValue("grading-mode", out var mode))
            {
                executeArgs.GradingConfig = mode.ToUpperInvariant() switch
                {
                    "CLIENT" => GradingConfig.ClientOnly,
                    "SERVER" => GradingConfig.ServerOnly,
                    "CONSOLE" => GradingConfig.ConsoleOutputOnly,
                    "HTTP" => GradingConfig.HttpTrafficOnly,
                    _ => GradingConfig.Default
                };
            }
            else
            {
                executeArgs.GradingConfig = GradingConfig.Default;
            }
            
            // Set timeouts if specified
            if (args.TryGetValue("timeout", out var timeoutStr) && int.TryParse(timeoutStr, out var timeout))
            {
                executeArgs.GradingConfig.StageTimeoutSeconds = Math.Max(1, timeout);
            }
            
            Console.WriteLine($"Suite Path: {executeArgs.SuitePath}");
            Console.WriteLine($"Results will be saved to: {timestampedResultRoot}");
            Console.WriteLine();
            
            // ===================================================================
            // NEW: Use modular architecture with ProcessLauncher pattern
            // GradingOrchestrator delegates to specialized services
            // ===================================================================
            Console.WriteLine("Using modular architecture (Phase 1 complete)");
            Console.WriteLine("ProcessLauncher pattern: Service orchestration");
            Console.WriteLine();
            
            var orchestrator = new GradingOrchestrator(timestampedResultRoot);
            
            // Execute suite using new modular architecture
            var result = orchestrator.ExecuteGrading(executeArgs);
            
            // Return exit code
            if (!result.Success)
            {
                return -1; // Critical error
            }
            
            return result.EarnedMarks >= result.TotalMarks * 0.5 ? 0 : 1;
        }
        
        /// <summary>
        /// Parses command-line arguments into a dictionary
        /// </summary>
        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (!arg.StartsWith("--")) continue;
                
                var key = arg.TrimStart('-');
                
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    map[key] = args[i + 1];
                    i++; // Skip next argument as it's the value
                }
                else
                {
                    map[key] = "true"; // Flag without value
                }
            }
            
            return map;
        }
        
        /// <summary>
        /// Prints usage information
        /// </summary>
        private static int PrintUsage()
        {
            Console.WriteLine(@"
USAGE:
  Client ExecuteSuite --suite <path> --out <path> [options]

REQUIRED ARGUMENTS:
  --suite <path>         Path to test suite folder (contains header.xlsx and environment.xlsx)
  --out <path>           Path to output folder where results will be saved

OPTIONAL ARGUMENTS:
  --client <path>        Path to student's client executable (exe or dll)
  --server <path>        Path to student's server executable (exe or dll)
  --client-appsettings   Path to client appsettings.json template
  --server-appsettings   Path to server appsettings.json template
  --db-script <path>     Path to database script (overrides test kit default)
  --timeout <seconds>    Stage timeout in seconds (default: 30)
  --grading-mode <mode>  Grading mode (default, client, server, console, http)

GRADING MODES:
  default   - Validate all aspects (client, server, network traffic)
  client    - Validate only client-side output
  server    - Validate only server-side output
  console   - Validate only console outputs (no network validation)
  http      - Validate only network traffic (no console validation)

EXAMPLES:
  # Grade with both client and server
  Client ExecuteSuite --suite ./MyTestCases/Testkit_HTTP_1 --out ./Results --client ./student/client.exe --server ./student/server.exe

  # Grade client only
  Client ExecuteSuite --suite ./MyTestCases/Testkit_HTTP_1 --out ./Results --client ./student/client.exe --grading-mode client

  # Grade with custom timeout
  Client ExecuteSuite --suite ./MyTestCases/Testkit_HTTP_1 --out ./Results --client ./student/client.exe --timeout 60

NOTES:
  - Test suite structure is read from header.xlsx and environment.xlsx
  - Database reset is automatic if database script is available
  - Results are saved in timestamped folders (GradeResult_YYYYMMDD_HHMMSS)
  - Detailed logs are generated: GradeProcess.log and GradeResults.xlsx
  - Network traffic capture requires middleware (currently stub implementation)

For more information, see README.md
");
            return -1;
        }
    }
}

