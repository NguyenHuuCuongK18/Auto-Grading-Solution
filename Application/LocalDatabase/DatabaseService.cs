using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using LocalLog;

namespace LocalDatabase
{
    /// <summary>
    /// Service for database operations, primarily database reset
    /// Handles SQL Server database initialization and reset for grading
    /// </summary>
    public class DatabaseService
    {
        private readonly Logger _logger;
        
        public DatabaseService(Logger logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Resets database using SQL script
        /// Injects DROP block if not present
        /// </summary>
        public bool ResetDatabase(string scriptPath, string connectionString)
        {
            try
            {
                _logger.LogProcess($"Resetting database using script: {scriptPath}");
                
                if (!File.Exists(scriptPath))
                {
                    _logger.LogError($"Database script not found: {scriptPath}");
                    return false;
                }
                
                // Read SQL script
                var sqlScript = File.ReadAllText(scriptPath);
                
                // Check if ALTER DATABASE...DROP IF EXISTS block is present
                if (!HasDropBlock(sqlScript))
                {
                    _logger.LogProcess("SQL script missing DROP block - injecting...");
                    sqlScript = InjectDropBlock(sqlScript, connectionString);
                    
                    // Save modified script
                    File.WriteAllText(scriptPath, sqlScript);
                    _logger.LogProcess("SQL script updated with DROP block");
                }
                
                // Execute SQL script
                ExecuteSqlScript(sqlScript, connectionString);
                
                _logger.LogProcess("Database reset completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database reset failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if SQL script has ALTER DATABASE...DROP IF EXISTS block
        /// </summary>
        private bool HasDropBlock(string sqlScript)
        {
            // Look for patterns like: ALTER DATABASE ... SET SINGLE_USER ... DROP DATABASE IF EXISTS
            var pattern = @"ALTER\s+DATABASE.*SET\s+SINGLE_USER|DROP\s+DATABASE\s+IF\s+EXISTS";
            return Regex.IsMatch(sqlScript, pattern, RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Injects ALTER DATABASE DROP block at the beginning of the script
        /// </summary>
        private string InjectDropBlock(string sqlScript, string connectionString)
        {
            // Extract database name from connection string
            var builder = new SqlConnectionStringBuilder(connectionString);
            var dbName = builder.InitialCatalog;
            
            if (string.IsNullOrEmpty(dbName))
            {
                throw new Exception("Cannot determine database name from connection string");
            }
            
            var dropBlock = $@"
-- AUTO-GENERATED: Drop database if exists
USE master;
GO

ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

DROP DATABASE IF EXISTS [{dbName}];
GO

";
            
            return dropBlock + sqlScript;
        }
        
        /// <summary>
        /// Executes SQL script by splitting on GO statements
        /// </summary>
        private void ExecuteSqlScript(string sqlScript, string connectionString)
        {
            // Split script on GO statements
            var batches = Regex.Split(sqlScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            
            foreach (var batch in batches)
            {
                var trimmedBatch = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmedBatch)) continue;
                
                try
                {
                    using var command = new SqlCommand(trimmedBatch, connection);
                    command.CommandTimeout = 120; // 2 minutes timeout
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"SQL batch execution warning: {ex.Message}");
                    // Continue with other batches
                }
            }
        }
    }
}
