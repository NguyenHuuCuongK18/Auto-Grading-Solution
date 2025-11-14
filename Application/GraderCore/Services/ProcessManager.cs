using System.Diagnostics;
using System.Text;
using GraderCore.Abstractions;

namespace GraderCore.Services
{
    /// <summary>
    /// Manages executable processes (client, server, middleware)
    /// Captures console output and handles process lifecycle
    /// </summary>
    public class ProcessManager : IProcessManager
    {
        private readonly Dictionary<string, ManagedProcess> _processes = new();
        private readonly object _lock = new();
        
        public bool StartProcess(string processName, string exePath, string workingDirectory, string? arguments = null)
        {
            lock (_lock)
            {
                // Stop existing process with same name if any
                if (_processes.ContainsKey(processName))
                {
                    StopProcess(processName);
                }
                
                try
                {
                    var managed = new ManagedProcess
                    {
                        Name = processName,
                        OutputBuffer = new StringBuilder(),
                        ErrorBuffer = new StringBuilder()
                    };
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = arguments ?? string.Empty,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    managed.Process = new Process { StartInfo = startInfo };
                    
                    // Setup output/error handlers
                    managed.Process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            lock (managed.OutputBuffer)
                            {
                                managed.OutputBuffer.AppendLine(e.Data);
                            }
                        }
                    };
                    
                    managed.Process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            lock (managed.ErrorBuffer)
                            {
                                managed.ErrorBuffer.AppendLine(e.Data);
                            }
                        }
                    };
                    
                    // Start process
                    if (!managed.Process.Start())
                    {
                        return false;
                    }
                    
                    // Begin reading output
                    managed.Process.BeginOutputReadLine();
                    managed.Process.BeginErrorReadLine();
                    
                    _processes[processName] = managed;
                    
                    // Give process a moment to start
                    Thread.Sleep(500);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to start process {processName}: {ex.Message}");
                    return false;
                }
            }
        }
        
        public void SendInput(string processName, string input)
        {
            lock (_lock)
            {
                if (_processes.TryGetValue(processName, out var managed) && 
                    managed.Process != null && 
                    !managed.Process.HasExited)
                {
                    try
                    {
                        managed.Process.StandardInput.WriteLine(input);
                        managed.Process.StandardInput.Flush();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to send input to {processName}: {ex.Message}");
                    }
                }
            }
        }
        
        public string GetOutput(string processName)
        {
            lock (_lock)
            {
                if (_processes.TryGetValue(processName, out var managed))
                {
                    string output;
                    lock (managed.OutputBuffer)
                    {
                        output = managed.OutputBuffer.ToString();
                        managed.OutputBuffer.Clear();
                    }
                    
                    // Also include error output
                    lock (managed.ErrorBuffer)
                    {
                        if (managed.ErrorBuffer.Length > 0)
                        {
                            output += managed.ErrorBuffer.ToString();
                            managed.ErrorBuffer.Clear();
                        }
                    }
                    
                    return output;
                }
                return string.Empty;
            }
        }
        
        public void StopProcess(string processName)
        {
            lock (_lock)
            {
                if (_processes.TryGetValue(processName, out var managed))
                {
                    try
                    {
                        if (managed.Process != null && !managed.Process.HasExited)
                        {
                            managed.Process.Kill(entireProcessTree: true);
                            managed.Process.WaitForExit(2000);
                        }
                        managed.Process?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error stopping process {processName}: {ex.Message}");
                    }
                    finally
                    {
                        _processes.Remove(processName);
                    }
                }
            }
        }
        
        public bool IsProcessRunning(string processName)
        {
            lock (_lock)
            {
                return _processes.TryGetValue(processName, out var managed) &&
                       managed.Process != null &&
                       !managed.Process.HasExited;
            }
        }
        
        public void StopAllProcesses()
        {
            lock (_lock)
            {
                foreach (var processName in _processes.Keys.ToList())
                {
                    StopProcess(processName);
                }
            }
        }
        
        private class ManagedProcess
        {
            public string Name { get; set; } = string.Empty;
            public Process? Process { get; set; }
            public StringBuilder OutputBuffer { get; set; } = new();
            public StringBuilder ErrorBuffer { get; set; } = new();
        }
    }
}
