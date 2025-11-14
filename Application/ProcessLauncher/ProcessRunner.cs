using System.Diagnostics;
using System.Text;

namespace ProcessLauncher
{
    /// <summary>
    /// Process runner that supports both .exe and .dll execution
    /// Handles process lifecycle and output capture
    /// </summary>
    public class ProcessRunner
    {
        /// <summary>
        /// Runs a process and captures both stdout and stderr
        /// Supports .exe files and .dll files (via dotnet)
        /// </summary>
        /// <param name="executable">Path to .exe or .dll file</param>
        /// <param name="arguments">Command line arguments</param>
        /// <param name="workingDirectory">Working directory for the process</param>
        /// <param name="timeout">Timeout in milliseconds (0 for infinite)</param>
        /// <returns>Tuple of (stdout, stderr, exitCode)</returns>
        public static async Task<(string stdout, string stderr, int exitCode)> RunAsync(
            string executable,
            string arguments = "",
            string? workingDirectory = null,
            int timeout = 0)
        {
            var processInfo = PrepareProcessStartInfo(executable, arguments, workingDirectory);
            
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            
            using var process = new Process();
            process.StartInfo = processInfo;
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    stdout.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    stderr.AppendLine(e.Data);
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            bool exited;
            if (timeout > 0)
            {
                exited = await Task.Run(() => process.WaitForExit(timeout));
                if (!exited)
                {
                    process.Kill(true);
                    return (stdout.ToString(), $"Process timed out after {timeout}ms\n" + stderr.ToString(), -1);
                }
            }
            else
            {
                await process.WaitForExitAsync();
            }
            
            return (stdout.ToString(), stderr.ToString(), process.ExitCode);
        }
        
        /// <summary>
        /// Starts a long-running process (server, middleware, etc.)
        /// Returns the Process object for further management
        /// </summary>
        public static Process StartLongRunningProcess(
            string executable,
            string arguments = "",
            string? workingDirectory = null,
            Action<string>? onOutputReceived = null,
            Action<string>? onErrorReceived = null)
        {
            var processInfo = PrepareProcessStartInfo(executable, arguments, workingDirectory);
            
            var process = new Process();
            process.StartInfo = processInfo;
            
            if (onOutputReceived != null)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        onOutputReceived(e.Data);
                };
            }
            
            if (onErrorReceived != null)
            {
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        onErrorReceived(e.Data);
                };
            }
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            return process;
        }
        
        /// <summary>
        /// Prepares ProcessStartInfo for both .exe and .dll execution
        /// </summary>
        private static ProcessStartInfo PrepareProcessStartInfo(
            string executable,
            string arguments,
            string? workingDirectory)
        {
            var info = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Check if it's a .dll file - if so, use dotnet to run it
            if (executable.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                info.FileName = "dotnet";
                info.Arguments = $"\"{executable}\" {arguments}";
            }
            else
            {
                info.FileName = executable;
                info.Arguments = arguments;
            }
            
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                info.WorkingDirectory = workingDirectory;
            }
            
            return info;
        }
        
        /// <summary>
        /// Sends input to a running process
        /// </summary>
        public static async Task SendInputAsync(Process process, string input)
        {
            if (process != null && !process.HasExited)
            {
                await process.StandardInput.WriteLineAsync(input);
                await process.StandardInput.FlushAsync();
            }
        }
        
        /// <summary>
        /// Kills a process and all its children
        /// </summary>
        public static void KillProcessTree(Process? process)
        {
            if (process == null || process.HasExited)
                return;
                
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process may have already exited
            }
        }
    }
}
