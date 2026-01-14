using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HyZaap.Services
{
    public class JavaService
    {
        public class JavaVersionInfo
        {
            public bool IsInstalled { get; set; }
            public string Version { get; set; } = string.Empty;
            public string JavaPath { get; set; } = string.Empty;
            public bool IsVersion25 { get; set; }
        }

        public async Task<JavaVersionInfo> CheckJavaInstallationAsync()
        {
            return await Task.Run(() =>
            {
                var info = new JavaVersionInfo();

                // FIRST: Check local java/ directory next to executable (highest priority)
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var localJavaDir = Path.Combine(appDir, "java");
                if (Directory.Exists(localJavaDir))
                {
                    // Try recursive search first
                    var javaExe = FindJavaExecutableRecursive(localJavaDir);
                    if (string.IsNullOrEmpty(javaExe))
                    {
                        javaExe = FindJavaExecutable(localJavaDir);
                    }
                    
                    if (!string.IsNullOrEmpty(javaExe) && File.Exists(javaExe))
                    {
                        var version = CheckJavaVersion(javaExe);
                        if (!string.IsNullOrEmpty(version))
                        {
                            info.IsInstalled = true;
                            info.JavaPath = javaExe;
                            info.Version = version.Trim();

                            // Parse version - try multiple patterns
                            var versionMatch = Regex.Match(version, @"(?:openjdk|java)\s+(\d+)\.(\d+)\.(\d+)", RegexOptions.IgnoreCase);
                            if (!versionMatch.Success)
                            {
                                // Try pattern like "25.0.1"
                                versionMatch = Regex.Match(version, @"(\d+)\.(\d+)\.(\d+)");
                            }

                            if (versionMatch.Success)
                            {
                                var majorVersion = int.Parse(versionMatch.Groups[1].Value);
                                info.IsVersion25 = majorVersion >= 25;
                            }
                            else
                            {
                                // If we can't parse but it's in local java/, assume it's version 25
                                info.IsVersion25 = true;
                            }
                            
                            // Found local Java, return it immediately
                            return info;
                        }
                        else
                        {
                            // Java exe found but version check failed - still accept it if in local directory
                            info.IsInstalled = true;
                            info.JavaPath = javaExe;
                            info.Version = "Java (version check failed)";
                            info.IsVersion25 = true; // Trust local Java
                            return info;
                        }
                    }
                }

                // SECOND: Try to find java in PATH (only if local Java not found)
                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "java",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processStartInfo);
                    if (process != null)
                    {
                        var output = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            info.IsInstalled = true;
                            info.JavaPath = "java"; // Use PATH java
                            info.Version = output.Trim();

                            // Parse version - try multiple patterns
                            var versionMatch = Regex.Match(output, @"(?:openjdk|java)\s+(\d+)\.(\d+)\.(\d+)", RegexOptions.IgnoreCase);
                            if (!versionMatch.Success)
                            {
                                // Try pattern like "25.0.1"
                                versionMatch = Regex.Match(output, @"(\d+)\.(\d+)\.(\d+)");
                            }
                            
                            if (versionMatch.Success)
                            {
                                var majorVersion = int.Parse(versionMatch.Groups[1].Value);
                                info.IsVersion25 = majorVersion >= 25;
                            }
                            else
                            {
                                // If we can't parse, assume it might be version 25 if installed
                                info.IsVersion25 = true; // Let user proceed, server will fail if wrong version
                            }
                            
                            return info;
                        }
                    }
                }
                catch
                {
                    // Java not in PATH, continue to check common installation locations
                }

                // THIRD: Check common Java installation paths (only if local and PATH Java not found)
                if (!info.IsInstalled)
                {
                    var commonPaths = new List<string>
                    {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Java"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Eclipse Adoptium"),
                    };

                    foreach (var basePath in commonPaths)
                    {
                        if (Directory.Exists(basePath))
                        {
                            var javaExe = FindJavaExecutable(basePath);
                            if (!string.IsNullOrEmpty(javaExe))
                            {
                                var version = CheckJavaVersion(javaExe);
                                if (!string.IsNullOrEmpty(version))
                                {
                                    info.IsInstalled = true;
                                    info.JavaPath = javaExe;
                                    info.Version = version.Trim();

                                    // Parse version - try multiple patterns
                                    var versionMatch = Regex.Match(version, @"(?:openjdk|java)\s+(\d+)\.(\d+)\.(\d+)", RegexOptions.IgnoreCase);
                                    if (!versionMatch.Success)
                                    {
                                        // Try pattern like "25.0.1"
                                        versionMatch = Regex.Match(version, @"(\d+)\.(\d+)\.(\d+)");
                                    }

                                    if (versionMatch.Success)
                                    {
                                        var majorVersion = int.Parse(versionMatch.Groups[1].Value);
                                        info.IsVersion25 = majorVersion >= 25;
                                    }
                                    else
                                    {
                                        // If we can't parse, assume it might be version 25 if installed
                                        info.IsVersion25 = true; // Let user proceed, server will fail if wrong version
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                return info;
            });
        }

        private string? FindJavaExecutable(string basePath)
        {
            try
            {
                // First check if java.exe is directly in bin/ subdirectory
                var directJavaExe = Path.Combine(basePath, "bin", "java.exe");
                if (File.Exists(directJavaExe))
                {
                    return directJavaExe;
                }

                // Look for jdk-* or jre-* folders
                var dirs = Directory.GetDirectories(basePath);
                foreach (var dir in dirs)
                {
                    var javaExe = Path.Combine(dir, "bin", "java.exe");
                    if (File.Exists(javaExe))
                    {
                        return javaExe;
                    }

                    // Also check subdirectories (sometimes nested)
                    var subDirs = Directory.GetDirectories(dir);
                    foreach (var subDir in subDirs)
                    {
                        var subJavaExe = Path.Combine(subDir, "bin", "java.exe");
                        if (File.Exists(subJavaExe))
                        {
                            return subJavaExe;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private string? CheckJavaVersion(string javaPath)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    var output = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode == 0 ? output : null;
                }
            }
            catch { }

            return null;
        }

        private string? FindJavaExecutableRecursive(string basePath)
        {
            try
            {
                // Recursively search for java.exe
                var files = Directory.GetFiles(basePath, "java.exe", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    // Prefer files in bin/ directories
                    var binFiles = Array.FindAll(files, f => f.Contains("\\bin\\") || f.Contains("/bin/"));
                    if (binFiles.Length > 0)
                    {
                        return binFiles[0];
                    }
                    return files[0];
                }
            }
            catch { }

            return null;
        }

        public string GetJavaDownloadUrl()
        {
            return "https://adoptium.net/temurin/releases/?version=25";
        }
    }
}

