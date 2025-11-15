using System.Text;
using ClosedXML.Excel;
using GraderCore.Abstractions;
using GraderCore.Keywords;
using GraderCore.Models;

namespace GraderCore.Services
{
    /// <summary>
    /// Logging service for process logs and result reports
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly StringBuilder _processLog = new();
        private readonly Dictionary<string, List<StageResult>> _stageResults = new();
        private readonly object _lock = new();
        
        public void LogProcess(string message, string level = "INFO")
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] [{level}] {message}";
                _processLog.AppendLine(logLine);
                Console.WriteLine(logLine);
            }
        }
        
        public void LogStageResult(string testCaseId, StageResult stageResult)
        {
            lock (_lock)
            {
                if (!_stageResults.ContainsKey(testCaseId))
                {
                    _stageResults[testCaseId] = new List<StageResult>();
                }
                _stageResults[testCaseId].Add(stageResult);
            }
        }
        
        public void GenerateReports(SuiteGradingResult suiteResult, string outputPath)
        {
            Directory.CreateDirectory(outputPath);
            
            // Generate Excel results (includes process log and summary)
            GenerateExcelResults(suiteResult, outputPath);
        }
        
        /// <summary>
        /// Generates detailed Excel results file
        /// </summary>
        private void GenerateExcelResults(SuiteGradingResult suiteResult, string outputPath)
        {
            var excelPath = Path.Combine(outputPath, FileKeywords.LogFile_GradeResults);
            
            using var workbook = new XLWorkbook();
            
            // Process Log sheet
            CreateProcessLogSheet(workbook);
            
            // Summary sheet
            CreateSummarySheet(workbook, suiteResult);
            
            // Detailed sheets for each test case
            foreach (var tcResult in suiteResult.TestCaseResults)
            {
                if (_stageResults.TryGetValue(tcResult.TestCaseId, out var stages))
                {
                    CreateDetailSheet(workbook, tcResult.TestCaseId, stages);
                }
            }
            
            workbook.SaveAs(excelPath);
            LogProcess($"Results Excel saved to: {excelPath}");
        }
        
        /// <summary>
        /// Creates the process log sheet with all logged messages
        /// </summary>
        private void CreateProcessLogSheet(XLWorkbook workbook)
        {
            var sheet = workbook.Worksheets.Add("ProcessLog");
            
            sheet.Cell(1, 1).Value = "Timestamp";
            sheet.Cell(1, 2).Value = "Level";
            sheet.Cell(1, 3).Value = "Message";
            
            // Format header
            var headerRange = sheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            
            // Parse log entries
            var logLines = _processLog.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            int row = 2;
            
            foreach (var line in logLines)
            {
                // Parse log format: [timestamp] [level] message
                var match = System.Text.RegularExpressions.Regex.Match(line, @"^\[(.*?)\] \[(.*?)\] (.*)$");
                if (match.Success)
                {
                    sheet.Cell(row, 1).Value = match.Groups[1].Value;
                    sheet.Cell(row, 2).Value = match.Groups[2].Value;
                    sheet.Cell(row, 3).Value = match.Groups[3].Value;
                    
                    // Color code by level
                    var level = match.Groups[2].Value;
                    if (level == "ERROR")
                    {
                        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                    }
                    else if (level == "WARN")
                    {
                        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }
                }
                else
                {
                    // Line doesn't match expected format, put in message column
                    sheet.Cell(row, 3).Value = line;
                }
                row++;
            }
            
            sheet.Columns().AdjustToContents();
        }
        
        /// <summary>
        /// Creates the summary sheet with overall test results
        /// </summary>
        private void CreateSummarySheet(XLWorkbook workbook, SuiteGradingResult suiteResult)
        {
            var summarySheet = workbook.Worksheets.Add("Summary");
            
            // Add header information
            summarySheet.Cell(1, 1).Value = "AUTO-GRADING RESULTS";
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(1, 1).Style.Font.FontSize = 14;
            summarySheet.Range(1, 1, 1, 5).Merge();
            
            summarySheet.Cell(2, 1).Value = "Start Time:";
            summarySheet.Cell(2, 2).Value = suiteResult.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
            summarySheet.Cell(3, 1).Value = "End Time:";
            summarySheet.Cell(3, 2).Value = suiteResult.EndTime.ToString("yyyy-MM-dd HH:mm:ss");
            summarySheet.Cell(4, 1).Value = "Duration:";
            summarySheet.Cell(4, 2).Value = $"{(suiteResult.EndTime - suiteResult.StartTime).TotalSeconds:F2} seconds";
            summarySheet.Cell(5, 1).Value = "Total Score:";
            summarySheet.Cell(5, 2).Value = $"{suiteResult.TotalEarnedMarks:F2} / {suiteResult.TotalMaxMarks:F2} ({suiteResult.PercentageScore:F2}%)";
            summarySheet.Cell(5, 2).Style.Font.Bold = true;
            
            // Test case results table
            int row = 7;
            summarySheet.Cell(row, 1).Value = "Test Case";
            summarySheet.Cell(row, 2).Value = "Max Marks";
            summarySheet.Cell(row, 3).Value = "Earned Marks";
            summarySheet.Cell(row, 4).Value = "Passed";
            summarySheet.Cell(row, 5).Value = "Summary";
            
            // Format table header
            var tableHeaderRange = summarySheet.Range(row, 1, row, 5);
            tableHeaderRange.Style.Font.Bold = true;
            tableHeaderRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            
            row++;
            foreach (var tcResult in suiteResult.TestCaseResults)
            {
                summarySheet.Cell(row, 1).Value = tcResult.TestCaseId;
                summarySheet.Cell(row, 2).Value = tcResult.MaxMarks;
                summarySheet.Cell(row, 3).Value = tcResult.EarnedMarks;
                summarySheet.Cell(row, 4).Value = tcResult.Passed ? "PASS" : "FAIL";
                summarySheet.Cell(row, 5).Value = tcResult.Summary;
                
                // Color code PASS/FAIL
                if (tcResult.Passed)
                {
                    summarySheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.LightGreen;
                }
                else
                {
                    summarySheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.LightPink;
                }
                row++;
            }
            
            // Total row
            summarySheet.Cell(row, 1).Value = "TOTAL";
            summarySheet.Cell(row, 1).Style.Font.Bold = true;
            summarySheet.Cell(row, 2).Value = suiteResult.TotalMaxMarks;
            summarySheet.Cell(row, 3).Value = suiteResult.TotalEarnedMarks;
            summarySheet.Cell(row, 4).Value = $"{suiteResult.PercentageScore:F2}%";
            summarySheet.Cell(row, 4).Style.Font.Bold = true;
            
            // Add critical errors if any
            if (suiteResult.CriticalErrors.Count > 0)
            {
                row += 2;
                summarySheet.Cell(row, 1).Value = "CRITICAL ERRORS:";
                summarySheet.Cell(row, 1).Style.Font.Bold = true;
                summarySheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.Red;
                summarySheet.Cell(row, 1).Style.Font.FontColor = XLColor.White;
                row++;
                
                foreach (var error in suiteResult.CriticalErrors)
                {
                    summarySheet.Cell(row, 1).Value = error;
                    summarySheet.Range(row, 1, row, 5).Merge();
                    row++;
                }
            }
            
            summarySheet.Columns().AdjustToContents();
        }
        
        /// <summary>
        /// Creates a detailed sheet for a test case showing stage-by-stage results
        /// </summary>
        private void CreateDetailSheet(XLWorkbook workbook, string testCaseId, List<StageResult> stages)
        {
            // Sanitize sheet name (max 31 chars, no special chars)
            var sheetName = testCaseId.Length > 31 ? testCaseId.Substring(0, 31) : testCaseId;
            sheetName = sheetName.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
            
            var sheet = workbook.Worksheets.Add(sheetName);
            
            sheet.Cell(1, 1).Value = "Stage";
            sheet.Cell(1, 2).Value = "Action";
            sheet.Cell(1, 3).Value = "Passed";
            sheet.Cell(1, 4).Value = "Expected Client";
            sheet.Cell(1, 5).Value = "Actual Client";
            sheet.Cell(1, 6).Value = "Expected Server";
            sheet.Cell(1, 7).Value = "Actual Server";
            sheet.Cell(1, 8).Value = "Validation Messages";
            
            int row = 2;
            foreach (var stage in stages)
            {
                sheet.Cell(row, 1).Value = stage.StageNumber;
                sheet.Cell(row, 2).Value = stage.Action;
                sheet.Cell(row, 3).Value = stage.Passed ? "PASS" : "FAIL";
                sheet.Cell(row, 4).Value = stage.ExpectedClientConsole ?? "";
                sheet.Cell(row, 5).Value = stage.ActualClientConsole ?? "";
                sheet.Cell(row, 6).Value = stage.ExpectedServerConsole ?? "";
                sheet.Cell(row, 7).Value = stage.ActualServerConsole ?? "";
                sheet.Cell(row, 8).Value = string.Join("; ", stage.ValidationMessages);
                row++;
            }
            
            // Format header
            var headerRange = sheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
            
            sheet.Columns().AdjustToContents();
        }
    }
}
