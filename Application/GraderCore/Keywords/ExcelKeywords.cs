namespace GraderCore.Keywords
{
    /// <summary>
    /// Excel sheet names and column keywords for test kit files
    /// </summary>
    public static class ExcelKeywords
    {
        // Suite header.xlsx sheets
        public static class SuiteHeader
        {
            public const string Sheet_DataPattern = "DataPattern";
            public const string Sheet_QuestionMark = "QuestionMark";
            public const string Sheet_TestSuite = "TestSuite";
            public const string Sheet_Config = "Config";
            
            public const string Col_Cases = "Cases";
            public const string Col_Mark = "Mark";
            public const string Col_TestCaseID = "TestCaseID";
            public const string Col_Description = "Description";
        }
        
        // Suite environment.xlsx sheets
        public static class SuiteEnvironment
        {
            public const string Sheet_Config = "Config";
            public const string Sheet_Action = "Action";
            public const string Sheet_Run = "Run";
            
            public const string Col_Key = "Key";
            public const string Col_Value = "Value";
            
            // Config keys
            public const string Key_EnvironmentType = "Environment_Type";
            public const string Key_AppType = "App_Type";
            public const string Key_CodeInternalPort = "Code_Container_Internal_Port";
            public const string Key_CodeHostPort = "Code_Container_Host_Port";
            public const string Key_DatabaseUsername = "Database_Username";
            public const string Key_DatabasePassword = "Database_Password";
            public const string Key_DefaultDatabaseName = "Default_Database_Name";
            public const string Key_DefaultDatabaseFilePath = "Default_Database_File_Path";
            public const string Key_RuntimesFolder = "Runtimes_Folder";
            public const string Key_Given = "Given";
        }
        
        // Test case header.xlsx sheets
        public static class TestCaseHeader
        {
            public const string Sheet_TestcaseProperty = "Testcase_Property";
            public const string Sheet_BrowserConfig = "Browser_Config";
            
            public const string Key_TestCaseID = "Test Case ID";
            public const string Key_ExecutionDate = "Execution Date";
            public const string Key_Timeout = "Timeout(Seconds)";
            public const string Key_Domain = "Domain";
            public const string Key_GradeContent = "Grade_Content";
        }
        
        // Test case detail.xlsx sheets and columns
        public static class TestCaseDetail
        {
            public const string Sheet_User = "User";
            public const string Sheet_Client = "Client";
            public const string Sheet_Server = "Server";
            public const string Sheet_Network = "Network";
            
            // User sheet columns
            public const string Col_Stage = "Stage";
            public const string Col_Input = "Input";
            public const string Col_Action = "Action";
            
            // Client sheet columns
            public const string Col_Console = "Console";
            
            // Server sheet columns (same as Client)
            // Col_Console
            
            // Network sheet columns
            public const string Col_Url = "Url";
            public const string Col_HttpMethod = "HttpMethod";
            public const string Col_REQ_Payload = "REQ_Payload";
            public const string Col_RES_Payload = "RES_Payload";
        }
    }
}
