namespace GraderCore.Models
{
    /// <summary>
    /// Results from grading a test case
    /// </summary>
    public class TestCaseResult
    {
        /// <summary>
        /// Test case ID
        /// </summary>
        public string TestCaseId { get; set; } = string.Empty;
        
        /// <summary>
        /// Maximum marks possible
        /// </summary>
        public double MaxMarks { get; set; }
        
        /// <summary>
        /// Marks earned
        /// </summary>
        public double EarnedMarks { get; set; }
        
        /// <summary>
        /// Whether test case passed completely
        /// </summary>
        public bool Passed { get; set; }
        
        /// <summary>
        /// Summary message
        /// </summary>
        public string Summary { get; set; } = string.Empty;
        
        /// <summary>
        /// Results for each stage
        /// </summary>
        public List<StageResult> StageResults { get; set; } = new();
        
        /// <summary>
        /// Any errors encountered during test case execution
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
    
    /// <summary>
    /// Results from a single stage execution
    /// </summary>
    public class StageResult
    {
        /// <summary>
        /// Stage number
        /// </summary>
        public int StageNumber { get; set; }
        
        /// <summary>
        /// Action performed
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether stage passed
        /// </summary>
        public bool Passed { get; set; }
        
        /// <summary>
        /// Actual client console output captured
        /// </summary>
        public string? ActualClientConsole { get; set; }
        
        /// <summary>
        /// Expected client console output
        /// </summary>
        public string? ExpectedClientConsole { get; set; }
        
        /// <summary>
        /// Whether client console matched (null if not checked)
        /// </summary>
        public bool? ClientConsoleMatched { get; set; }
        
        /// <summary>
        /// Actual server console output captured
        /// </summary>
        public string? ActualServerConsole { get; set; }
        
        /// <summary>
        /// Expected server console output
        /// </summary>
        public string? ExpectedServerConsole { get; set; }
        
        /// <summary>
        /// Whether server console matched (null if not checked)
        /// </summary>
        public bool? ServerConsoleMatched { get; set; }
        
        /// <summary>
        /// Actual network data captured
        /// </summary>
        public NetworkActual? ActualNetwork { get; set; }
        
        /// <summary>
        /// Expected network data
        /// </summary>
        public NetworkExpectation? ExpectedNetwork { get; set; }
        
        /// <summary>
        /// Whether network data matched (null if not checked)
        /// </summary>
        public bool? NetworkMatched { get; set; }
        
        /// <summary>
        /// Validation errors/differences for this stage
        /// </summary>
        public List<string> ValidationMessages { get; set; } = new();
    }
    
    /// <summary>
    /// Actual network data captured during stage execution
    /// </summary>
    public class NetworkActual
    {
        /// <summary>
        /// Actual URL accessed
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// Actual HTTP method used
        /// </summary>
        public string HttpMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// Actual request payload sent
        /// </summary>
        public string RequestPayload { get; set; } = string.Empty;
        
        /// <summary>
        /// Actual response payload received
        /// </summary>
        public string ResponsePayload { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Overall suite grading results
    /// </summary>
    public class SuiteGradingResult
    {
        /// <summary>
        /// Total marks possible across all test cases
        /// </summary>
        public double TotalMaxMarks { get; set; }
        
        /// <summary>
        /// Total marks earned
        /// </summary>
        public double TotalEarnedMarks { get; set; }
        
        /// <summary>
        /// Percentage score
        /// </summary>
        public double PercentageScore => TotalMaxMarks > 0 ? (TotalEarnedMarks / TotalMaxMarks) * 100 : 0;
        
        /// <summary>
        /// Results for each test case
        /// </summary>
        public List<TestCaseResult> TestCaseResults { get; set; } = new();
        
        /// <summary>
        /// Critical errors that stopped grading
        /// </summary>
        public List<string> CriticalErrors { get; set; } = new();
        
        /// <summary>
        /// Timestamp when grading started
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// Timestamp when grading completed
        /// </summary>
        public DateTime EndTime { get; set; }
    }
}
