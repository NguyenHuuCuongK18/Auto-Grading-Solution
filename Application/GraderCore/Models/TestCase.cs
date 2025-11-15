namespace GraderCore.Models
{
    /// <summary>
    /// Represents a single test case with its configuration and expected data
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Test case ID (e.g., "TC01_Start")
        /// </summary>
        public string TestCaseId { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to the test case folder
        /// </summary>
        public string TestCasePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Marks allocated to this test case
        /// </summary>
        public double Marks { get; set; }
        
        /// <summary>
        /// Test case configuration (from header.xlsx)
        /// </summary>
        public TestCaseConfig Config { get; set; } = new();
        
        /// <summary>
        /// List of execution stages with expected data
        /// </summary>
        public List<TestStage> Stages { get; set; } = new();
    }
    
    /// <summary>
    /// Test case configuration from header.xlsx
    /// </summary>
    public class TestCaseConfig
    {
        /// <summary>
        /// Timeout in seconds for this test case
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;
        
        /// <summary>
        /// Domain/base URL (e.g., "http://localhost:5235")
        /// </summary>
        public string Domain { get; set; } = string.Empty;
        
        /// <summary>
        /// What to grade: "Client", "Server", or "Both"
        /// </summary>
        public string GradeContent { get; set; } = "Client";
    }
    
    /// <summary>
    /// Represents a single stage in test execution
    /// A stage typically corresponds to a user action (StartClient, Input, etc.)
    /// </summary>
    public class TestStage
    {
        /// <summary>
        /// Stage number (sequential, starting from 1)
        /// </summary>
        public int StageNumber { get; set; }
        
        /// <summary>
        /// User action for this stage (from User sheet)
        /// </summary>
        public UserAction? UserAction { get; set; }
        
        /// <summary>
        /// Expected client console output (from Client sheet)
        /// Null if sheet doesn't have this stage or sheet is missing
        /// </summary>
        public string? ExpectedClientConsole { get; set; }
        
        /// <summary>
        /// Expected server console output (from Server sheet)
        /// Null if sheet doesn't have this stage or sheet is missing
        /// </summary>
        public string? ExpectedServerConsole { get; set; }
        
        /// <summary>
        /// Expected network data (from Network sheet)
        /// Null if sheet doesn't have this stage or sheet is missing
        /// </summary>
        public NetworkExpectation? ExpectedNetwork { get; set; }
    }
    
    /// <summary>
    /// User action to perform in a stage
    /// </summary>
    public class UserAction
    {
        /// <summary>
        /// Action type (StartClient, StartServer, CloseClient, CloseServer, Input)
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// Input text (only for Input action)
        /// </summary>
        public string Input { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Expected network data for a stage
    /// </summary>
    public class NetworkExpectation
    {
        /// <summary>
        /// Expected URL
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// Expected HTTP method (GET, POST, etc.)
        /// </summary>
        public string HttpMethod { get; set; } = "GET";
        
        /// <summary>
        /// Expected request payload
        /// </summary>
        public string RequestPayload { get; set; } = string.Empty;
        
        /// <summary>
        /// Expected response payload
        /// </summary>
        public string ResponsePayload { get; set; } = string.Empty;
    }
}
