using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HyZaap.Models;

namespace HyZaap.Services
{
    public class ServerProcessService
    {
        public event EventHandler<string>? OutputReceived;
        public event EventHandler? ProcessExited;

        public async Task<bool> StartServerAsync(ServerInstance server)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (server.IsRunning)
                    {
                        return false;
                    }

                    var serverJarPath = Path.Combine(server.ServerPath, "Server", "HytaleServer.jar");
                    if (!File.Exists(serverJarPath))
                    {
                        OutputReceived?.Invoke(this, $"ERROR: HytaleServer.jar not found at {serverJarPath}");
                        return false;
                    }

                    var assetsPath = server.AssetsPath;
                    if (string.IsNullOrEmpty(assetsPath))
                    {
                        assetsPath = Path.Combine(server.ServerPath, "Assets.zip");
                    }

                    if (!File.Exists(assetsPath))
                    {
                        OutputReceived?.Invoke(this, $"ERROR: Assets.zip not found at {assetsPath}");
                        return false;
                    }

                    // Build Java arguments
                    var javaArgs = new StringBuilder();

                    // Memory settings
                    javaArgs.Append($"-Xms{server.MinMemoryMB}M -Xmx{server.MaxMemoryMB}M ");

                    // AOT Cache
                    if (server.UseAOTCache)
                    {
                        var aotCachePath = Path.Combine(server.ServerPath, "Server", "HytaleServer.aot");
                        if (File.Exists(aotCachePath))
                        {
                            javaArgs.Append($"-XX:AOTCache=\"{aotCachePath}\" ");
                        }
                    }

                    // JAR and server arguments
                    javaArgs.Append($"-jar \"{serverJarPath}\" ");
                    javaArgs.Append($"--assets \"{assetsPath}\" ");
                    javaArgs.Append($"--bind {server.BindAddress}:{server.Port} ");

                    if (server.DisableSentry)
                    {
                        javaArgs.Append("--disable-sentry ");
                    }

                    if (server.EnableBackups)
                    {
                        javaArgs.Append("--backup ");
                        if (!string.IsNullOrEmpty(server.BackupDirectory))
                        {
                            javaArgs.Append($"--backup-dir \"{server.BackupDirectory}\" ");
                        }
                        javaArgs.Append($"--backup-frequency {server.BackupFrequencyMinutes} ");
                    }

                    if (server.AuthMode == "offline")
                    {
                        javaArgs.Append("--auth-mode offline ");
                    }

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = string.IsNullOrEmpty(server.JavaPath) ? "java" : server.JavaPath,
                        Arguments = javaArgs.ToString().Trim(),
                        WorkingDirectory = Path.Combine(server.ServerPath, "Server"),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var process = new Process
                    {
                        StartInfo = processStartInfo,
                        EnableRaisingEvents = true
                    };

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            OutputReceived?.Invoke(this, e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            OutputReceived?.Invoke(this, e.Data);
                        }
                    };

                    process.Exited += (sender, e) =>
                    {
                        server.IsRunning = false;
                        server.Process = null;
                        server.ProcessId = null;
                        ProcessExited?.Invoke(this, EventArgs.Empty);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    server.Process = process;
                    server.ProcessId = process.Id;
                    server.IsRunning = true;
                    server.LastStarted = DateTime.Now;

                    return true;
                }
                catch (Exception ex)
                {
                    OutputReceived?.Invoke(this, $"ERROR: Failed to start server: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> StopServerAsync(ServerInstance server)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!server.IsRunning)
                    {
                        return false;
                    }

                    Process? process = server.Process;
                    
                    // If we don't have the process object, try to get it by ID
                    if (process == null && server.ProcessId.HasValue)
                    {
                        try
                        {
                            process = Process.GetProcessById(server.ProcessId.Value);
                            if (process.HasExited)
                            {
                                server.IsRunning = false;
                                server.ProcessId = null;
                                return true;
                            }
                        }
                        catch
                        {
                            // Process doesn't exist anymore
                            server.IsRunning = false;
                            server.ProcessId = null;
                            return true;
                        }
                    }

                    if (process == null)
                    {
                        OutputReceived?.Invoke(this, "ERROR: Cannot find server process.");
                        return false;
                    }

                    // Check if we can use StandardInput (only if we started the process ourselves)
                    bool canUseStandardInput = false;
                    try
                    {
                        if (!process.HasExited)
                        {
                            var stream = process.StandardInput;
                            canUseStandardInput = stream != null && !stream.BaseStream.CanWrite == false;
                        }
                    }
                    catch
                    {
                        // StandardInput is not available (process was started by another instance)
                        canUseStandardInput = false;
                    }

                    if (canUseStandardInput)
                    {
                        // Try graceful shutdown first
                        try
                        {
                            process.StandardInput.WriteLine("stop");
                            process.StandardInput.Flush();

                            // Wait up to 10 seconds for graceful shutdown
                            if (!process.WaitForExit(10000))
                            {
                                // Force kill if still running
                                process.Kill();
                                process.WaitForExit();
                            }
                        }
                        catch
                        {
                            // If StandardInput fails, fall back to kill
                            if (!process.HasExited)
                            {
                                process.Kill();
                                process.WaitForExit();
                            }
                        }
                    }
                    else
                    {
                        // Can't use StandardInput, use taskkill on Windows or kill signal
                        if (!process.HasExited)
                        {
                            try
                            {
                                // On Windows, use taskkill for graceful termination
                                var killProcess = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = "taskkill",
                                        Arguments = $"/PID {process.Id} /T /F",
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true
                                    }
                                };
                                killProcess.Start();
                                killProcess.WaitForExit();
                                
                                // Wait a bit for the process to actually exit
                                if (!process.WaitForExit(5000))
                                {
                                    // If still running, force kill
                                    process.Kill();
                                    process.WaitForExit();
                                }
                            }
                            catch
                            {
                                // Fallback to direct kill
                                if (!process.HasExited)
                                {
                                    process.Kill();
                                    process.WaitForExit();
                                }
                            }
                        }
                    }

                    server.IsRunning = false;
                    server.Process = null;
                    server.ProcessId = null;

                    return true;
                }
                catch (Exception ex)
                {
                    OutputReceived?.Invoke(this, $"ERROR: Failed to stop server: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> SendCommandAsync(ServerInstance server, string command)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!server.IsRunning)
                    {
                        return false;
                    }

                    Process? process = server.Process;
                    
                    // If we don't have the process object, try to get it by ID
                    if (process == null && server.ProcessId.HasValue)
                    {
                        try
                        {
                            process = Process.GetProcessById(server.ProcessId.Value);
                            if (process.HasExited)
                            {
                                server.IsRunning = false;
                                server.ProcessId = null;
                                return false;
                            }
                        }
                        catch
                        {
                            server.IsRunning = false;
                            server.ProcessId = null;
                            return false;
                        }
                    }

                    if (process == null || process.HasExited)
                    {
                        return false;
                    }

                    // Try to send command via StandardInput
                    try
                    {
                        process.StandardInput.WriteLine(command);
                        process.StandardInput.Flush();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        OutputReceived?.Invoke(this, $"ERROR: Cannot send command (process was started by another instance): {ex.Message}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    OutputReceived?.Invoke(this, $"ERROR: Failed to send command: {ex.Message}");
                    return false;
                }
            });
        }

        public bool CheckProcessRunning(ServerInstance server)
        {
            try
            {
                if (server.ProcessId == null)
                {
                    return false;
                }

                var process = Process.GetProcessById(server.ProcessId.Value);
                if (process.HasExited)
                {
                    return false;
                }

                // Try to verify it's actually our server process by checking command line
                try
                {
                    var commandLine = GetCommandLine(process);
                    if (commandLine.Contains("HytaleServer.jar") && commandLine.Contains($"--bind {server.BindAddress}:{server.Port}"))
                    {
                        // Reattach to the process if we don't have it
                        if (server.Process == null)
                        {
                            server.Process = process;
                            // Note: We can't reattach output streams, but at least we can control it
                        }
                        return true;
                    }
                }
                catch
                {
                    // If we can't check command line, assume it's running if process exists
                    if (server.Process == null)
                    {
                        server.Process = process;
                    }
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GetCommandLine(Process process)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
                foreach (System.Management.ManagementBaseObject obj in searcher.Get())
                {
                    return obj["CommandLine"]?.ToString() ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }
    }
}

