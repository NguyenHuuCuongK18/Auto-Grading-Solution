using ClosedXML.Excel;
using LocalGraderConfig.Models;
using LocalGraderConfig.Keywords;

namespace SingleStudentGrade.ExcelParsers
{
    /// <summary>
    /// Loads test suite configuration from header.xlsx and environment.xlsx
    /// Migrated from GraderCore to SingleStudentGrade as part of Phase 2 refactoring
    /// </summary>
    public class SuiteLoader
    {
        /// <summary>
        /// Loads test suite from the specified path
        /// </summary>
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
        
        private void LoadHeaderFile(string headerPath, TestSuite suite)
        {
            using var workbook = new XLWorkbook(headerPath);
            
            if (workbook.TryGetWorksheet(ExcelKeywords.SuiteHeader.Sheet_QuestionMark, out var markSheet))
            {
                var rows = markSheet.RowsUsed().Skip(1);
                
                foreach (var row in rows)
                {
                    var testCaseId = row.Cell(1).GetString();
                    var markCell = row.Cell(2);
                    
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
        
        private void LoadEnvironmentFile(string envPath, TestSuite suite)
        {
            using var workbook = new XLWorkbook(envPath);
            
            if (workbook.TryGetWorksheet(ExcelKeywords.SuiteEnvironment.Sheet_Config, out var configSheet))
            {
                var env = suite.Environment;
                var rows = configSheet.RowsUsed().Skip(1);
                
                foreach (var row in rows)
                {
                    var key = row.Cell(1).GetString();
                    var value = row.Cell(2).GetString();
                    
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    
                    env.AllConfig[key] = value;
                    
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
