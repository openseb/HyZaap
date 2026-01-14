using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HyZaap.Services
{
    public class ServerDownloadService
    {
        public event EventHandler<string>? ProgressUpdate;
        public event EventHandler<int>? ProgressPercentage;

        public async Task<bool> DownloadServerFilesAsync(string downloadPath, string serverDirectory)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProgressUpdate?.Invoke(this, "Checking for Hytale Downloader...");

                    // Check if hytale-downloader exists in the application directory
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    var downloaderPath = Path.Combine(appDir, "hytale-downloader.exe");

                    if (!File.Exists(downloaderPath))
                    {
                        ProgressUpdate?.Invoke(this, "Hytale Downloader not found. Please download it from the Hytale documentation.");
                        return false;
                    }

                    ProgressUpdate?.Invoke(this, "Starting server download...");
                    ProgressPercentage?.Invoke(this, 0);

                    // Ensure server directory exists
                    Directory.CreateDirectory(serverDirectory);

                    // Run hytale-downloader
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = downloaderPath,
                        Arguments = $"-download-path \"{Path.Combine(serverDirectory, "game.zip")}\"",
                        WorkingDirectory = serverDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processStartInfo);
                    if (process == null)
                    {
                        ProgressUpdate?.Invoke(this, "Failed to start downloader process.");
                        return false;
                    }

                    // Read output
                    string? line;
                    while ((line = process.StandardOutput.ReadLine()) != null)
                    {
                        ProgressUpdate?.Invoke(this, line);
                    }

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        ProgressUpdate?.Invoke(this, $"Download failed: {error}");
                        return false;
                    }

                    ProgressUpdate?.Invoke(this, "Extracting server files...");
                    ProgressPercentage?.Invoke(this, 50);

                    // Extract the downloaded zip
                    var zipPath = Path.Combine(serverDirectory, "game.zip");
                    if (File.Exists(zipPath))
                    {
                        ZipFile.ExtractToDirectory(zipPath, serverDirectory, true);
                        File.Delete(zipPath);
                    }

                    ProgressUpdate?.Invoke(this, "Server files downloaded successfully!");
                    ProgressPercentage?.Invoke(this, 100);

                    return true;
                }
                catch (Exception ex)
                {
                    ProgressUpdate?.Invoke(this, $"Error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> CopyFromLauncherAsync(string launcherPath, string serverDirectory)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ProgressUpdate?.Invoke(this, "Copying files from launcher installation...");
                    ProgressPercentage?.Invoke(this, 0);

                    var serverSourcePath = Path.Combine(launcherPath, "Server");
                    var assetsSourcePath = Path.Combine(launcherPath, "Assets.zip");

                    if (!Directory.Exists(serverSourcePath))
                    {
                        ProgressUpdate?.Invoke(this, "Server folder not found in launcher directory.");
                        return false;
                    }

                    if (!File.Exists(assetsSourcePath))
                    {
                        ProgressUpdate?.Invoke(this, "Assets.zip not found in launcher directory.");
                        return false;
                    }

                    Directory.CreateDirectory(serverDirectory);

                    // Copy Server folder
                    ProgressUpdate?.Invoke(this, "Copying Server folder...");
                    CopyDirectory(serverSourcePath, Path.Combine(serverDirectory, "Server"), true);
                    ProgressPercentage?.Invoke(this, 50);

                    // Copy Assets.zip
                    ProgressUpdate?.Invoke(this, "Copying Assets.zip...");
                    File.Copy(assetsSourcePath, Path.Combine(serverDirectory, "Assets.zip"), true);
                    ProgressPercentage?.Invoke(this, 100);

                    ProgressUpdate?.Invoke(this, "Files copied successfully!");
                    return true;
                }
                catch (Exception ex)
                {
                    ProgressUpdate?.Invoke(this, $"Error: {ex.Message}");
                    return false;
                }
            });
        }

        private void CopyDirectory(string sourceDir, string destDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}

