using ClosedXML.Excel;
using GraderCore.Abstractions;
using GraderCore.Keywords;
using GraderCore.Models;

namespace GraderCore.Services
{
    /// <summary>
    /// Loads test suite configuration from header.xlsx and environment.xlsx
    /// </summary>
    public class SuiteLoader : ISuiteLoader
    {
        public TestSuite LoadSuite(string suitePath)
        {
            if (!Directory.Exists(suitePath))
            {
                throw new DirectoryNotFoundException($"Suite path not found: {suitePath}");
            }
            
            var suite = new TestSuite { SuitePath = suitePath };
            
            // Load header.xlsx for test case marks
            var headerPath = Path.Combine(suitePath, FileKeywords.SuiteHeaderFile);
            if (File.Exists(headerPath))
            {
                LoadHeaderFile(headerPath, suite);
            }
            else
            {
                throw new FileNotFoundException($"Suite header file not found: {headerPath}");
            }
            
            // Load environment.xlsx for configuration
            var envPath = Path.Combine(suitePath, FileKeywords.SuiteEnvironmentFile);
            if (File.Exists(envPath))
            {
                LoadEnvironmentFile(envPath, suite);
            }
            else
            {
                throw new FileNotFoundException($"Suite environment file not found: {envPath}");
            }
            
            return suite;
        }
        
        /// <summary>
        /// Loads test case marks from header.xlsx QuestionMark sheet
        /// </summary>
        private void LoadHeaderFile(string headerPath, TestSuite suite)
        {
            using var workbook = new XLWorkbook(headerPath);
            
            // Read QuestionMark sheet for test case marks
            if (workbook.TryGetWorksheet(ExcelKeywords.SuiteHeader.Sheet_QuestionMark, out var markSheet))
            {
                // Skip header row, read data rows
                var rows = markSheet.RowsUsed().Skip(1);
                
                foreach (var row in rows)
                {
                    var testCaseId = row.Cell(1).GetString(); // Column A: Cases
                    var markCell = row.Cell(2);               // Column B: Mark
                    
                    if (!string.IsNullOrWhiteSpace(testCaseId))
                    {
                        try
                        {
                            var mark = markCell.GetDouble();
                            suite.TestCaseMarks[testCaseId] = mark;
                        }
                        catch
                        {
                            // Skip rows where mark is not a valid number
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"QuestionMark sheet not found in {headerPath}");
            }
        }
        
        /// <summary>
        /// Loads environment configuration from environment.xlsx Config sheet
        /// </summary>
        private void LoadEnvironmentFile(string envPath, TestSuite suite)
        {
            using var workbook = new XLWorkbook(envPath);
            
            // Read Config sheet
            if (workbook.TryGetWorksheet(ExcelKeywords.SuiteEnvironment.Sheet_Config, out var configSheet))
            {
                var env = suite.Environment;
                
                // Skip header row, read key-value pairs
                var rows = configSheet.RowsUsed().Skip(1);
                
                foreach (var row in rows)
                {
                    var key = row.Cell(1).GetString();   // Column A: Key
                    var value = row.Cell(2).GetString(); // Column B: Value
                    
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    
                    // Store in dictionary for extensibility
                    env.AllConfig[key] = value;
                    
                    // Map to specific properties
                    switch (key)
                    {
                        case ExcelKeywords.SuiteEnvironment.Key_EnvironmentType:
                            env.EnvironmentType = value;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_AppType:
                            env.AppType = value;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_CodeInternalPort:
                            if (int.TryParse(value, out var internalPort))
                                env.CodeContainerInternalPort = internalPort;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_CodeHostPort:
                            if (int.TryParse(value, out var hostPort))
                                env.CodeContainerHostPort = hostPort;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_DatabaseUsername:
                            env.DatabaseUsername = value;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_DatabasePassword:
                            env.DatabasePassword = value;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_DefaultDatabaseName:
                            env.DefaultDatabaseName = value;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_DefaultDatabaseFilePath:
                            env.DefaultDatabaseFilePath = value;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_RuntimesFolder:
                            env.RuntimesFolder = value;
                            break;
                        case ExcelKeywords.SuiteEnvironment.Key_Given:
                            env.GivenFolder = value;
                            break;
                    }
                }
            }
            else
            {
                throw new Exception($"Config sheet not found in {envPath}");
            }
        }
    }
}
