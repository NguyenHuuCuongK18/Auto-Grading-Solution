namespace GraderCore.Keywords
{
    /// <summary>
    /// Keywords and patterns for file names and paths used throughout the grading system
    /// </summary>
    public static class FileKeywords
    {
        // Suite-level files (outermost directory)
        public const string SuiteHeaderFile = "header.xlsx";
        public const string SuiteEnvironmentFile = "environment.xlsx";
        
        // Test case level files (inside each TC folder)
        public const string TestCaseHeaderFile = "header.xlsx";
        public const string TestCaseDetailFile = "detail.xlsx";
        public const string TestCaseEnvironmentFile = "environment.xlsx";
        
        // Result folder pattern
        public const string Pattern_GradeResult = "GradeResult_{0}";
        
        // Log file names
        public const string LogFile_GradeProcess = "GradeProcess.log";
        public const string LogFile_GradeResults = "GradeResults.xlsx";
        
        // Meta folder
        public const string MetaFolder = "Meta";
        public const string GivenFolder = "Given";
        public const string RuntimesFolder = "runtimes";
        
        // Database
        public const string DefaultDatabaseScriptFile = "database.sql";
    }
}
