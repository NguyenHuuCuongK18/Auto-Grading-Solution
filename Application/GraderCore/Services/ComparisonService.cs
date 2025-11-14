using GraderCore.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraderCore.Services
{
    /// <summary>
    /// Compares expected vs actual data with normalization and difference detection
    /// </summary>
    public class ComparisonService : IComparisonService
    {
        public ComparisonResult Compare(string expected, string actual, bool normalize = true)
        {
            var result = new ComparisonResult();
            
            if (normalize)
            {
                expected = NormalizeText(expected);
                actual = NormalizeText(actual);
            }
            
            // Direct string comparison
            if (expected == actual)
            {
                result.Matched = true;
                return result;
            }
            
            // Try JSON comparison if both look like JSON
            if (IsJson(expected) && IsJson(actual))
            {
                return CompareJson(expected, actual);
            }
            
            // Text comparison with differences
            result.Matched = false;
            FindTextDifferences(expected, actual, result);
            
            return result;
        }
        
        /// <summary>
        /// Normalizes text by trimming whitespace and normalizing line endings
        /// </summary>
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            // Normalize line endings
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Trim each line and the whole string
            var lines = text.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l));
            
            return string.Join("\n", lines);
        }
        
        /// <summary>
        /// Checks if a string is valid JSON
        /// </summary>
        private bool IsJson(string text)
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text)) return false;
            
            return (text.StartsWith("{") && text.EndsWith("}")) ||
                   (text.StartsWith("[") && text.EndsWith("]"));
        }
        
        /// <summary>
        /// Compares two JSON strings semantically
        /// </summary>
        private ComparisonResult CompareJson(string expected, string actual)
        {
            var result = new ComparisonResult();
            
            try
            {
                var expectedObj = JToken.Parse(expected);
                var actualObj = JToken.Parse(actual);
                
                if (JToken.DeepEquals(expectedObj, actualObj))
                {
                    result.Matched = true;
                }
                else
                {
                    result.Matched = false;
                    result.Differences.Add("JSON structures differ");
                    result.DifferenceExcerpt = $"Expected: {expected.Substring(0, Math.Min(100, expected.Length))}...\nActual: {actual.Substring(0, Math.Min(100, actual.Length))}...";
                }
            }
            catch
            {
                // Fall back to text comparison if JSON parsing fails
                result.Matched = false;
                result.Differences.Add("Failed to parse JSON");
            }
            
            return result;
        }
        
        /// <summary>
        /// Finds differences between two text strings
        /// </summary>
        private void FindTextDifferences(string expected, string actual, ComparisonResult result)
        {
            var expectedLines = expected.Split('\n');
            var actualLines = actual.Split('\n');
            
            if (expectedLines.Length != actualLines.Length)
            {
                result.Differences.Add($"Line count differs: expected {expectedLines.Length}, got {actualLines.Length}");
            }
            
            var maxLines = Math.Max(expectedLines.Length, actualLines.Length);
            for (int i = 0; i < maxLines && result.Differences.Count < 10; i++)
            {
                var expLine = i < expectedLines.Length ? expectedLines[i] : "";
                var actLine = i < actualLines.Length ? actualLines[i] : "";
                
                if (expLine != actLine)
                {
                    result.Differences.Add($"Line {i + 1} differs");
                    if (string.IsNullOrEmpty(result.DifferenceExcerpt))
                    {
                        result.DifferenceExcerpt = $"Line {i + 1}:\nExpected: {expLine}\nActual:   {actLine}";
                    }
                }
            }
            
            if (result.Differences.Count == 0)
            {
                result.Differences.Add("Strings differ but no specific differences detected");
                result.DifferenceExcerpt = $"Expected length: {expected.Length}, Actual length: {actual.Length}";
            }
        }
    }
}
