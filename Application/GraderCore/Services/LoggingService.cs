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
            
            // Write process log
            var processLogPath = Path.Combine(outputPath, FileKeywords.LogFile_GradeProcess);
            File.WriteAllText(processLogPath, _processLog.ToString());
            LogProcess($"Process log saved to: {processLogPath}");
            
            // Generate Excel results
            GenerateExcelResults(suiteResult, outputPath);
            
            // Generate summary text file
            GenerateSummary(suiteResult, outputPath);
        }
        
        /// <summary>
        /// Generates detailed Excel results file
        /// </summary>
        private void GenerateExcelResults(SuiteGradingResult suiteResult, string outputPath)
        {
            var excelPath = Path.Combine(outputPath, FileKeywords.LogFile_GradeResults);
            
            using var workbook = new XLWorkbook();
            
            // Summary sheet
            var summarySheet = workbook.Worksheets.Add("Summary");
            summarySheet.Cell(1, 1).Value = "Test Case";
            summarySheet.Cell(1, 2).Value = "Max Marks";
            summarySheet.Cell(1, 3).Value = "Earned Marks";
            summarySheet.Cell(1, 4).Value = "Passed";
            summarySheet.Cell(1, 5).Value = "Summary";
            
            int row = 2;
            foreach (var tcResult in suiteResult.TestCaseResults)
            {
                summarySheet.Cell(row, 1).Value = tcResult.TestCaseId;
                summarySheet.Cell(row, 2).Value = tcResult.MaxMarks;
                summarySheet.Cell(row, 3).Value = tcResult.EarnedMarks;
                summarySheet.Cell(row, 4).Value = tcResult.Passed ? "PASS" : "FAIL";
                summarySheet.Cell(row, 5).Value = tcResult.Summary;
                row++;
            }
            
            // Total row
            summarySheet.Cell(row, 1).Value = "TOTAL";
            summarySheet.Cell(row, 2).Value = suiteResult.TotalMaxMarks;
            summarySheet.Cell(row, 3).Value = suiteResult.TotalEarnedMarks;
            summarySheet.Cell(row, 4).Value = $"{suiteResult.PercentageScore:F2}%";
            
            // Format header
            var headerRange = summarySheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            
            summarySheet.Columns().AdjustToContents();
            
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
        
        /// <summary>
        /// Generates a text summary file
        /// </summary>
        private void GenerateSummary(SuiteGradingResult suiteResult, string outputPath)
        {
            var summaryPath = Path.Combine(outputPath, "Summary.txt");
            var sb = new StringBuilder();
            
            sb.AppendLine("=================================");
            sb.AppendLine("   AUTO-GRADING RESULTS");
            sb.AppendLine("=================================");
            sb.AppendLine();
            sb.AppendLine($"Start Time: {suiteResult.StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"End Time: {suiteResult.EndTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Duration: {(suiteResult.EndTime - suiteResult.StartTime).TotalSeconds:F2} seconds");
            sb.AppendLine();
            sb.AppendLine($"Total Score: {suiteResult.TotalEarnedMarks:F2} / {suiteResult.TotalMaxMarks:F2} ({suiteResult.PercentageScore:F2}%)");
            sb.AppendLine();
            sb.AppendLine("Test Case Results:");
            sb.AppendLine("----------------------------------");
            
            foreach (var tcResult in suiteResult.TestCaseResults)
            {
                var status = tcResult.Passed ? "PASS" : "FAIL";
                sb.AppendLine($"  [{status}] {tcResult.TestCaseId}: {tcResult.EarnedMarks:F2}/{tcResult.MaxMarks:F2}");
                if (!string.IsNullOrEmpty(tcResult.Summary))
                {
                    sb.AppendLine($"        {tcResult.Summary}");
                }
            }
            
            if (suiteResult.CriticalErrors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("CRITICAL ERRORS:");
                sb.AppendLine("----------------------------------");
                foreach (var error in suiteResult.CriticalErrors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("=================================");
            
            File.WriteAllText(summaryPath, sb.ToString());
            LogProcess($"Summary saved to: {summaryPath}");
        }
    }
}
