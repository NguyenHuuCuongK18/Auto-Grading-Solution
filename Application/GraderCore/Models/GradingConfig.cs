namespace GraderCore.Models
{
    /// <summary>
    /// Configuration for grading execution
    /// Specifies what aspects should be validated during grading
    /// </summary>
    public class GradingConfig
    {
        /// <summary>
        /// Whether to validate client console output
        /// </summary>
        public bool ValidateClientConsole { get; set; } = true;
        
        /// <summary>
        /// Whether to validate server console output
        /// </summary>
        public bool ValidateServerConsole { get; set; } = true;
        
        /// <summary>
        /// Whether to validate network traffic (HTTP/TCP)
        /// </summary>
        public bool ValidateNetworkTraffic { get; set; } = true;
        
        /// <summary>
        /// Timeout in seconds for each stage execution
        /// </summary>
        public int StageTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Timeout in seconds for process startup
        /// </summary>
        public int ProcessStartTimeoutSeconds { get; set; } = 10;
        
        /// <summary>
        /// Default grading configuration - validates everything
        /// </summary>
        public static GradingConfig Default => new GradingConfig();
        
        /// <summary>
        /// Client-only grading - validates only client console and response data
        /// </summary>
        public static GradingConfig ClientOnly => new GradingConfig
        {
            ValidateServerConsole = false
        };
        
        /// <summary>
        /// Server-only grading - validates only server console and request data
        /// </summary>
        public static GradingConfig ServerOnly => new GradingConfig
        {
            ValidateClientConsole = false
        };
        
        /// <summary>
        /// Console-only grading - no network traffic validation
        /// </summary>
        public static GradingConfig ConsoleOutputOnly => new GradingConfig
        {
            ValidateNetworkTraffic = false
        };
        
        /// <summary>
        /// Network-only grading - no console output validation
        /// </summary>
        public static GradingConfig HttpTrafficOnly => new GradingConfig
        {
            ValidateClientConsole = false,
            ValidateServerConsole = false
        };
    }
}
