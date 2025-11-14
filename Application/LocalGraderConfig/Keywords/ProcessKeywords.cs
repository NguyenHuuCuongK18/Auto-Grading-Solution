namespace LocalGraderConfig.Keywords
{
    /// <summary>
    /// Keywords for process actions and management
    /// </summary>
    public static class ProcessKeywords
    {
        // User actions from detail.xlsx User sheet
        public const string Action_StartServer = "StartServer";
        public const string Action_StartClient = "StartClient";
        public const string Action_CloseServer = "CloseServer";
        public const string Action_CloseClient = "CloseClient";
        public const string Action_Input = "Input";
        
        // Grade content values
        public const string GradeContent_Client = "Client";
        public const string GradeContent_Server = "Server";
        public const string GradeContent_Both = "Both";
        
        // Process names for identification
        public const string ProcessName_Client = "Client";
        public const string ProcessName_Server = "Server";
        public const string ProcessName_Middleware = "Middleware";
    }
}
