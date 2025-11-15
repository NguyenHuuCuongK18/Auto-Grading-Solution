using GraderCore.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GraderCore.Services
{
    /// <summary>
    /// Enhanced comparison service with proven algorithms from reference implementation
    /// Provides robust text, JSON, and console output comparison with multiple fallback strategies
    /// Based on: https://github.com/NguyenHuuCuongK18/auto-grading.git DataComparisonService
    /// </summary>
    public class ComparisonService : IComparisonService
    {
        public ComparisonResult Compare(string expected, string actual, bool normalize = true)
        {
            var result = new ComparisonResult();
            
            // Handle empty expected (means validation is skipped)
            if (string.IsNullOrWhiteSpace(expected))
            {
                result.Matched = true;
                result.Differences.Add("Expected value is empty - validation skipped");
                return result;
            }
            
            // Handle empty actual (means nothing was captured)
            if (string.IsNullOrWhiteSpace(actual))
            {
                result.Matched = false;
                result.Differences.Add("Actual value is empty but expected value was provided");
                return result;
            }
            
            // Normalize both strings
            var exp = normalize ? NormalizeText(expected, caseInsensitive: true) : expected;
            var act = normalize ? NormalizeText(actual, caseInsensitive: true) : actual;
            
            // Strategy 1: Exact match after normalization
            if (exp == act)
            {
                result.Matched = true;
                result.Differences.Add("Exact match after normalization");
                return result;
            }
            
            // Strategy 2: Contains match (for console output - actual may have more than expected)
            if (act.Contains(exp))
            {
                result.Matched = true;
                result.Differences.Add("Loose match: actual contains expected");
                return result;
            }
            
            // Strategy 3: Aggressive normalization (remove all whitespace and punctuation)
            var expAggressive = StripAggressive(exp);
            var actAggressive = StripAggressive(act);
            
            if (expAggressive == actAggressive)
            {
                result.Matched = true;
                result.Differences.Add("Match after aggressive normalization");
                return result;
            }
            
            if (actAggressive.Contains(expAggressive))
            {
                result.Matched = true;
                result.Differences.Add("Loose match after aggressive normalization");
                return result;
            }
            
            // No match - find differences
            result.Matched = false;
            FindTextDifferences(exp, act, result);
            
            return result;
        }
        
        /// <summary>
        /// Advanced text normalization with multiple strategies
        /// Based on proven implementation from reference repository
        /// </summary>
        private string NormalizeText(string s, bool caseInsensitive)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            
            // 1. Strip BOM (Byte Order Mark)
            if (s.Length > 0 && s[0] == '\uFEFF') 
                s = s.Substring(1);
            
            // 2. Unescape Unicode escape sequences (\uXXXX)
            s = Regex.Replace(s, @"\\u([0-9a-fA-F]{4})", m =>
            {
                var code = Convert.ToInt32(m.Groups[1].Value, 16);
                return char.ConvertFromUtf32(code);
            });
            
            // 3. Normalize smart quotes and dashes to ASCII equivalents
            s = s.Replace("\u2018", "'").Replace("\u2019", "'")
                 .Replace("\u201C", "\"").Replace("\u201D", "\"")
                 .Replace("\u2013", "-").Replace("\u2014", "-");
            
            // 4. Try JSON canonicalization for JSON content
            if (s.TrimStart().StartsWith("{") || s.TrimStart().StartsWith("["))
            {
                try
                {
                    using var doc = JsonDocument.Parse(s);
                    s = JsonSerializer.Serialize(doc, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                }
                catch 
                { 
                    // Not valid JSON, continue with text normalization
                }
            }
            
            // 5. Convert all line endings to \n for consistency
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // 6. Replace all Unicode whitespace variants with regular spaces
            s = s.Replace("\u00A0", " ")  // Non-breaking space
                 .Replace("\u2002", " ")  // En space
                 .Replace("\u2003", " ")  // Em space
                 .Replace("\u2009", " ")  // Thin space
                 .Replace("\u200A", " ")  // Hair space
                 .Replace("\u202F", " ")  // Narrow no-break space
                 .Replace("\u205F", " ")  // Medium mathematical space
                 .Replace("\u3000", " ")  // Ideographic space
                 .Replace("\t", " ");      // Tab to space
            
            // 7. Trim leading/trailing whitespace from each line
            var lines = s.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }
            
            // 8. Remove empty lines completely to prevent whitespace/newline mismatches
            lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            s = string.Join(" ", lines); // Join with single space instead of newline
            
            // 9. Collapse multiple consecutive spaces into single space
            s = Regex.Replace(s, @"\s+", " ");
            
            // 10. Final trim
            s = s.Trim();
            
            return caseInsensitive ? s.ToLowerInvariant() : s;
        }
        
        /// <summary>
        /// Performs ultra-aggressive normalization by removing ALL whitespace and punctuation
        /// This is used as a last-resort fallback when standard normalization still shows differences
        /// </summary>
        private string StripAggressive(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            
            // Remove all whitespace characters
            s = Regex.Replace(s, @"\s+", "");
            
            // Remove common punctuation that may vary in output formatting
            s = s.Replace(",", "").Replace(".", "").Replace(":", "").Replace(";", "")
                 .Replace("!", "").Replace("?", "").Replace("'", "").Replace("\"", "");
            
            return s;
        }
        
        /// <summary>
        /// Finds first difference between two strings with context
        /// </summary>
        private void FindTextDifferences(string expected, string actual, ComparisonResult result)
        {
            var min = Math.Min(expected.Length, actual.Length);
            
            // Find first character difference
            for (int i = 0; i < min; i++)
            {
                if (expected[i] != actual[i])
                {
                    result.Differences.Add($"First difference at position {i}");
                    
                    // Extract context around the difference
                    var contextSize = 50;
                    var start = Math.Max(0, i - contextSize);
                    var expContext = expected.Substring(start, Math.Min(contextSize * 2, expected.Length - start));
                    var actContext = actual.Substring(start, Math.Min(contextSize * 2, actual.Length - start));
                    
                    result.DifferenceExcerpt = $"Position {i}:\nExpected: ...{expContext}...\nActual:   ...{actContext}...";
                    return;
                }
            }
            
            // If we get here, one string is a prefix of the other
            if (expected.Length != actual.Length)
            {
                result.Differences.Add($"Length differs: expected {expected.Length}, actual {actual.Length}");
                result.DifferenceExcerpt = $"One string is a prefix of the other.\nExpected length: {expected.Length}\nActual length: {actual.Length}";
            }
        }
    }
}
