namespace GraderCore.Models
{
    /// <summary>
    /// Arguments for executing a test suite
    /// Passed to the grading system via CLI or programmatically
    /// </summary>
    public class ExecuteSuiteArgs
    {
        /// <summary>
        /// Path to the test suite folder (contains header.xlsx, environment.xlsx, and test case folders)
        /// </summary>
        public string SuitePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Path where grading results will be saved
        /// </summary>
        public string ResultRoot { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to the student's client executable (exe or dll)
        /// </summary>
        public string? ClientExePath { get; set; }
        
        /// <summary>
        /// Path to the student's server executable (exe or dll)
        /// </summary>
        public string? ServerExePath { get; set; }
        
        /// <summary>
        /// Path to client appsettings.json template (optional)
        /// If not provided, will be generated from test kit configuration
        /// </summary>
        public string? ClientAppSettingsTemplate { get; set; }
        
        /// <summary>
        /// Path to server appsettings.json template (optional)
        /// If not provided, will be generated from test kit configuration
        /// </summary>
        public string? ServerAppSettingsTemplate { get; set; }
        
        /// <summary>
        /// Path to database script file (optional)
        /// If not provided, will use the path from environment.xlsx
        /// </summary>
        public string? DatabaseScriptPath { get; set; }
        
        /// <summary>
        /// Grading configuration (what to validate)
        /// </summary>
        public GradingConfig? GradingConfig { get; set; }
    }
}
