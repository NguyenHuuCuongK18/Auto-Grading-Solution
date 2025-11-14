namespace LocalGraderConfig.Models
{
    /// <summary>
    /// Test suite configuration loaded from header.xlsx and environment.xlsx
    /// </summary>
    public class TestSuite
    {
        /// <summary>
        /// Path to the suite folder
        /// </summary>
        public string SuitePath { get; set; } = string.Empty;
        
        /// <summary>
        /// List of test cases with their marks
        /// Key: Test Case ID (e.g., "TC01_Start")
        /// Value: Marks for the test case
        /// </summary>
        public Dictionary<string, double> TestCaseMarks { get; set; } = new();
        
        /// <summary>
        /// Environment configuration
        /// </summary>
        public EnvironmentConfig Environment { get; set; } = new();
    }
    
    /// <summary>
    /// Environment configuration loaded from environment.xlsx
    /// </summary>
    public class EnvironmentConfig
    {
        /// <summary>
        /// Environment type (e.g., "dotnet", "java")
        /// </summary>
        public string EnvironmentType { get; set; } = "dotnet";
        
        /// <summary>
        /// Application type (e.g., "console", "web")
        /// </summary>
        public string AppType { get; set; } = "console";
        
        /// <summary>
        /// Port for code container (internal)
        /// </summary>
        public int CodeContainerInternalPort { get; set; } = 8000;
        
        /// <summary>
        /// Port for code container (host)
        /// </summary>
        public int CodeContainerHostPort { get; set; } = 8001;
        
        /// <summary>
        /// Database username
        /// </summary>
        public string DatabaseUsername { get; set; } = "sa";
        
        /// <summary>
        /// Database password
        /// </summary>
        public string DatabasePassword { get; set; } = "sa";
        
        /// <summary>
        /// Default database name
        /// </summary>
        public string DefaultDatabaseName { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to default database script file (relative to suite)
        /// </summary>
        public string DefaultDatabaseFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to runtimes folder (relative to suite)
        /// </summary>
        public string RuntimesFolder { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to Given folder with standard client/server (relative to suite)
        /// </summary>
        public string GivenFolder { get; set; } = string.Empty;
        
        /// <summary>
        /// Full configuration dictionary for extensibility
        /// </summary>
        public Dictionary<string, string> AllConfig { get; set; } = new();
    }
}
