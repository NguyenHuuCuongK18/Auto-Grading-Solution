namespace LocalGrade
{
    /// <summary>
    /// Service for comparing expected vs actual data
    /// </summary>
    public interface IComparisonService
    {
        /// <summary>
        /// Compares two text strings (console output, JSON, etc.)
        /// </summary>
        /// <param name="expected">Expected text</param>
        /// <param name="actual">Actual text</param>
        /// <param name="normalize">Whether to normalize whitespace/formatting</param>
        /// <returns>Comparison result with match status and differences</returns>
        ComparisonResult Compare(string expected, string actual, bool normalize = true);
    }
    
    /// <summary>
    /// Result of a comparison operation
    /// </summary>
    public class ComparisonResult
    {
        /// <summary>
        /// Whether the values matched
        /// </summary>
        public bool Matched { get; set; }
        
        /// <summary>
        /// List of differences found
        /// </summary>
        public List<string> Differences { get; set; } = new();
        
        /// <summary>
        /// Excerpt highlighting the key difference
        /// </summary>
        public string? DifferenceExcerpt { get; set; }
    }
}
