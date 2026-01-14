using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace HyZaap.Services
{
    public class JavaDownloadService
    {
        public event EventHandler<string>? ProgressUpdate;
        public event EventHandler<int>? ProgressPercentage;

        public async Task<string?> DownloadJava25Async(string installDirectory)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    ProgressUpdate?.Invoke(this, "Determining Java download URL...");

                    // Create Java directory next to executable
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    var javaDir = Path.Combine(appDir, "java");
                    Directory.CreateDirectory(javaDir);

                    // For Windows x64, download from Adoptium API
                    // API endpoint that returns download URL
                    var apiUrl = "https://api.adoptium.net/v3/binary/latest/25/ga/windows/x64/jdk/hotspot/normal/eclipse";
                    var zipPath = Path.Combine(javaDir, "java.zip");

                    ProgressUpdate?.Invoke(this, "Fetching Java download URL...");
                    ProgressPercentage?.Invoke(this, 5);

                    // Use HttpClientHandler with redirect support
                    var handler = new HttpClientHandler
                    {
                        AllowAutoRedirect = true,
                        MaxAutomaticRedirections = 10
                    };
                    
                    using var httpClient = new HttpClient(handler);
                    httpClient.Timeout = TimeSpan.FromMinutes(15);

                    ProgressUpdate?.Invoke(this, "Downloading Java 25 (this may take a few minutes)...");
                    ProgressPercentage?.Invoke(this, 10);

                    // Download the file - API redirects to actual download
                    HttpResponseMessage response;
                    try
                    {
                        ProgressUpdate?.Invoke(this, "Connecting to Adoptium API...");
                        response = await httpClient.GetAsync(apiUrl, HttpCompletionOption.ResponseHeadersRead);
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            ProgressUpdate?.Invoke(this, $"API returned error: {response.StatusCode} {response.ReasonPhrase}");
                            return null;
                        }
                        
                        var location = response.Headers.Location?.ToString();
                        if (!string.IsNullOrEmpty(location) && location != apiUrl)
                        {
                            ProgressUpdate?.Invoke(this, $"Following redirect to download server...");
                            response.Dispose();
                            response = await httpClient.GetAsync(location, HttpCompletionOption.ResponseHeadersRead);
                            response.EnsureSuccessStatusCode();
                        }
                    }
                    catch (Exception ex)
                    {
                        ProgressUpdate?.Invoke(this, $"Failed to start download: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            ProgressUpdate?.Invoke(this, $"Inner error: {ex.InnerException.Message}");
                        }
                        return null;
                    }

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
                    
                    using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (totalBytes > 0)
                            {
                                var percent = (int)((totalBytesRead * 50) / totalBytes) + 10; // 10-60%
                                ProgressPercentage?.Invoke(this, percent);
                                ProgressUpdate?.Invoke(this, $"Downloading... {totalBytesRead / 1024 / 1024} MB / {totalBytes / 1024 / 1024} MB");
                            }
                        }
                        
                        await fileStream.FlushAsync();
                    }
                    
                    response.Dispose();
                    
                    // Verify file was downloaded
                    if (!File.Exists(zipPath))
                    {
                        ProgressUpdate?.Invoke(this, $"Error: Downloaded file not found at {zipPath}");
                        return null;
                    }
                    
                    var fileInfo = new FileInfo(zipPath);
                    ProgressUpdate?.Invoke(this, $"Download complete. File size: {fileInfo.Length / 1024 / 1024} MB");

                    ProgressUpdate?.Invoke(this, "Extracting Java...");
                    ProgressPercentage?.Invoke(this, 60);

                    // Extract the zip file
                    try
                    {
                        if (File.Exists(zipPath))
                        {
                            ProgressUpdate?.Invoke(this, $"Extracting {zipPath} to {javaDir}...");
                            ZipFile.ExtractToDirectory(zipPath, javaDir, true);
                            ProgressUpdate?.Invoke(this, "Extraction complete. Cleaning up...");
                            
                            // Delete the zip file
                            File.Delete(zipPath);
                            ProgressUpdate?.Invoke(this, "Zip file deleted.");
                        }
                        else
                        {
                            ProgressUpdate?.Invoke(this, $"Error: Zip file not found at {zipPath}");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        ProgressUpdate?.Invoke(this, $"Error extracting zip: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            ProgressUpdate?.Invoke(this, $"Inner error: {ex.InnerException.Message}");
                        }
                        return null;
                    }

                    ProgressUpdate?.Invoke(this, "Locating Java executable...");
                    ProgressPercentage?.Invoke(this, 80);

                    // Find the java.exe in the extracted folder
                    var javaExe = FindJavaExecutable(javaDir);
                    if (string.IsNullOrEmpty(javaExe))
                    {
                        // Try searching more thoroughly
                        ProgressUpdate?.Invoke(this, "Searching for java.exe in extracted files...");
                        javaExe = FindJavaExecutableRecursive(javaDir);
                    }
                    
                    if (string.IsNullOrEmpty(javaExe))
                    {
                        ProgressUpdate?.Invoke(this, $"Error: Could not find java.exe in downloaded package. Checked: {javaDir}");
                        ProgressUpdate?.Invoke(this, $"Please check the java/ directory manually. Files extracted to: {javaDir}");
                        return null;
                    }

                    ProgressUpdate?.Invoke(this, $"Java 25 installed successfully at: {javaExe}");
                    ProgressPercentage?.Invoke(this, 100);

                    return javaExe;
                }
                catch (Exception ex)
                {
                    ProgressUpdate?.Invoke(this, $"Error downloading Java: {ex.Message}");
                    return null;
                }
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
            catch (Exception ex)
            {
                ProgressUpdate?.Invoke(this, $"Error finding Java executable: {ex.Message}");
            }

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
                    var binFiles = Array.FindAll(files, f => f.Contains("\\bin\\"));
                    if (binFiles.Length > 0)
                    {
                        return binFiles[0];
                    }
                    return files[0];
                }
            }
            catch (Exception ex)
            {
                ProgressUpdate?.Invoke(this, $"Error in recursive search: {ex.Message}");
            }

            return null;
        }

        public string GetJavaDirectory(string serverDirectory)
        {
            // Place Java next to executable
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "java");
        }
    }
}

