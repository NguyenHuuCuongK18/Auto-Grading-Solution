using GraderCore.Abstractions;
using GraderCore.Models;

namespace GraderCore.Services
{
    /// <summary>
    /// Stub implementation of middleware service
    /// In a complete implementation, this would proxy HTTP/TCP traffic between client and server
    /// For now, returns null (no network capture) - network validation will be skipped
    /// </summary>
    public class MiddlewareService : IMiddlewareService
    {
        private readonly ILoggingService _logging;
        
        public MiddlewareService(ILoggingService logging)
        {
            _logging = logging;
        }
        
        public bool StartMiddleware(int clientPort, int serverPort)
        {
            _logging.LogProcess($"[STUB] Middleware service start requested (client port: {clientPort}, server port: {serverPort})", "WARN");
            _logging.LogProcess("Network traffic capture not yet implemented - network validation will be skipped", "WARN");
            return true; // Return true to not block execution
        }
        
        public void StopMiddleware()
        {
            _logging.LogProcess("[STUB] Middleware service stop requested", "INFO");
        }
        
        public NetworkActual? GetNetworkDataForStage(int stageNumber)
        {
            // No network data captured in stub implementation
            return null;
        }
        
        public void ClearNetworkData()
        {
            // No-op in stub
        }
    }
}
