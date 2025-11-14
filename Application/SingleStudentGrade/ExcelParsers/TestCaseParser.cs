using ClosedXML.Excel;
using LocalGraderConfig.Keywords;
using LocalGraderConfig.Models;

namespace SingleStudentGrade.ExcelParsers
{
    /// <summary>
    /// Parses test case configuration and expected data from test case folder.
    /// Migrated from GraderCore to SingleStudentGrade following ProcessLauncher pattern.
    /// </summary>
    public class TestCaseParser
    {
        /// <summary>
        /// Parses a test case from its folder, loading header.xlsx and detail.xlsx
        /// </summary>
        /// <param name="testCasePath">Path to test case folder</param>
        /// <param name="testCaseId">Test case identifier</param>
        /// <param name="marks">Marks allocated for this test case</param>
        /// <returns>Parsed TestCase with configuration and expected data</returns>
        public TestCase ParseTestCase(string testCasePath, string testCaseId, double marks)
        {
            if (!Directory.Exists(testCasePath))
            {
                throw new DirectoryNotFoundException($"Test case path not found: {testCasePath}");
            }
            
            var testCase = new TestCase
            {
                TestCaseId = testCaseId,
                TestCasePath = testCasePath,
                Marks = marks
            };
            
            // Load header.xlsx for test case configuration
            var headerPath = Path.Combine(testCasePath, FileKeywords.TestCaseHeaderFile);
            if (File.Exists(headerPath))
            {
                LoadTestCaseConfig(headerPath, testCase);
            }
            
            // Load detail.xlsx for test stages and expected data
            var detailPath = Path.Combine(testCasePath, FileKeywords.TestCaseDetailFile);
            if (File.Exists(detailPath))
            {
                LoadTestCaseDetail(detailPath, testCase);
            }
            else
            {
                throw new FileNotFoundException($"Detail file not found: {detailPath}");
            }
            
            return testCase;
        }
        
        /// <summary>
        /// Loads test case configuration from header.xlsx (Testcase_Property sheet)
        /// Reads timeout, domain, and grade content settings
        /// </summary>
        private void LoadTestCaseConfig(string headerPath, TestCase testCase)
        {
            using var workbook = new XLWorkbook(headerPath);
            
            // Read Testcase_Property sheet
            if (workbook.TryGetWorksheet(ExcelKeywords.TestCaseHeader.Sheet_TestcaseProperty, out var propSheet))
            {
                // Read key-value pairs (column A = key, column B = value)
                var rows = propSheet.RowsUsed();
                
                foreach (var row in rows)
                {
                    var key = row.Cell(1).GetString();
                    var value = row.Cell(2).GetString();
                    
                    switch (key)
                    {
                        case ExcelKeywords.TestCaseHeader.Key_Timeout:
                            if (int.TryParse(value, out var timeout))
                                testCase.Config.TimeoutSeconds = timeout;
                            break;
                        case ExcelKeywords.TestCaseHeader.Key_Domain:
                            testCase.Config.Domain = value;
                            break;
                        case ExcelKeywords.TestCaseHeader.Key_GradeContent:
                            testCase.Config.GradeContent = value;
                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Loads test stages and expected data from detail.xlsx.
        /// This is the main method that orchestrates loading all test expectations.
        /// </summary>
        private void LoadTestCaseDetail(string detailPath, TestCase testCase)
        {
            using var workbook = new XLWorkbook(detailPath);
            
            // First, load User sheet to get stage sequence and actions
            var userActions = LoadUserActions(workbook);
            
            // Create stages from user actions
            foreach (var (stageNum, action) in userActions)
            {
                var stage = new TestStage
                {
                    StageNumber = stageNum,
                    UserAction = action
                };
                testCase.Stages.Add(stage);
            }
            
            // Load expected client console output (if Client sheet exists)
            LoadClientExpectations(workbook, testCase.Stages);
            
            // Load expected server console output (if Server sheet exists)
            LoadServerExpectations(workbook, testCase.Stages);
            
            // Load expected network data (if Network sheet exists)
            LoadNetworkExpectations(workbook, testCase.Stages);
        }
        
        /// <summary>
        /// Loads user actions from User sheet.
        /// User actions define the test flow: StartServer, StartClient, Input, CloseServer, CloseClient
        /// </summary>
        /// <returns>Dictionary of stage number to user action</returns>
        private Dictionary<int, UserAction> LoadUserActions(XLWorkbook workbook)
        {
            var actions = new Dictionary<int, UserAction>();
            
            if (!workbook.TryGetWorksheet(ExcelKeywords.TestCaseDetail.Sheet_User, out var userSheet))
            {
                throw new Exception("User sheet not found in detail.xlsx");
            }
            
            // Skip header row
            var rows = userSheet.RowsUsed().Skip(1);
            
            foreach (var row in rows)
            {
                var stageCell = row.Cell(1);              // Stage column
                var input = row.Cell(2).GetString();      // Input column
                var action = row.Cell(3).GetString();     // Action column
                
                try
                {
                    var stageNum = (int)stageCell.GetDouble();
                    if (!string.IsNullOrWhiteSpace(action))
                    {
                        actions[stageNum] = new UserAction
                        {
                            Action = action.Trim(),
                            Input = input ?? string.Empty
                        };
                    }
                }
                catch
                {
                    // Skip invalid stage numbers (non-numeric or empty cells)
                }
            }
            
            return actions;
        }
        
        /// <summary>
        /// Loads expected client console output from Client sheet.
        /// Sheet may not exist - this means we're not validating client output for this test.
        /// This is by design to support partial test kits.
        /// </summary>
        private void LoadClientExpectations(XLWorkbook workbook, List<TestStage> stages)
        {
            // Client sheet may not exist - that's OK, it means we're not validating client output
            if (!workbook.TryGetWorksheet(ExcelKeywords.TestCaseDetail.Sheet_Client, out var clientSheet))
            {
                return;
            }
            
            // Skip header row
            var rows = clientSheet.RowsUsed().Skip(1);
            
            foreach (var row in rows)
            {
                var stageCell = row.Cell(1);              // Stage column
                var console = row.Cell(2).GetString();    // Console column
                
                try
                {
                    var stageNum = (int)stageCell.GetDouble();
                    var stage = stages.FirstOrDefault(s => s.StageNumber == stageNum);
                    if (stage != null)
                    {
                        stage.ExpectedClientConsole = console ?? string.Empty;
                    }
                }
                catch
                {
                    // Skip invalid stage numbers
                }
            }
        }
        
        /// <summary>
        /// Loads expected server console output from Server sheet.
        /// Sheet may not exist - this means we're not validating server output for this test.
        /// This is by design to support partial test kits.
        /// </summary>
        private void LoadServerExpectations(XLWorkbook workbook, List<TestStage> stages)
        {
            // Server sheet may not exist - that's OK
            if (!workbook.TryGetWorksheet(ExcelKeywords.TestCaseDetail.Sheet_Server, out var serverSheet))
            {
                return;
            }
            
            // Skip header row
            var rows = serverSheet.RowsUsed().Skip(1);
            
            foreach (var row in rows)
            {
                var stageCell = row.Cell(1);              // Stage column
                var console = row.Cell(2).GetString();    // Console column
                
                try
                {
                    var stageNum = (int)stageCell.GetDouble();
                    var stage = stages.FirstOrDefault(s => s.StageNumber == stageNum);
                    if (stage != null)
                    {
                        stage.ExpectedServerConsole = console ?? string.Empty;
                    }
                }
                catch
                {
                    // Skip invalid stage numbers
                }
            }
        }
        
        /// <summary>
        /// Loads expected network data from Network sheet.
        /// Sheet may not exist - this means we're not validating network traffic for this test.
        /// This is by design to support partial test kits.
        /// Network sheet contains: Stage, Url, REQ_Payload, RES_Payload columns
        /// </summary>
        private void LoadNetworkExpectations(XLWorkbook workbook, List<TestStage> stages)
        {
            // Network sheet may not exist - that's OK
            if (!workbook.TryGetWorksheet(ExcelKeywords.TestCaseDetail.Sheet_Network, out var networkSheet))
            {
                return;
            }
            
            // Skip header row
            var rows = networkSheet.RowsUsed().Skip(1);
            
            foreach (var row in rows)
            {
                var stageCell = row.Cell(1);                 // Stage column
                var url = row.Cell(2).GetString();           // Url column
                var reqPayload = row.Cell(3).GetString();    // REQ_Payload column
                var resPayload = row.Cell(4).GetString();    // RES_Payload column
                
                try
                {
                    var stageNum = (int)stageCell.GetDouble();
                    var stage = stages.FirstOrDefault(s => s.StageNumber == stageNum);
                    if (stage != null)
                    {
                        stage.ExpectedNetwork = new NetworkExpectation
                        {
                            Url = url ?? string.Empty,
                            RequestPayload = reqPayload ?? string.Empty,
                            ResponsePayload = resPayload ?? string.Empty
                        };
                    }
                }
                catch
                {
                    // Skip invalid stage numbers
                }
            }
        }
    }
}
