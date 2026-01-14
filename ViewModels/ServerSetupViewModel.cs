using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HyZaap.Models;
using HyZaap.Services;

namespace HyZaap.ViewModels
{
    public class ServerSetupViewModel : INotifyPropertyChanged
    {
        private readonly JavaService _javaService;
        private readonly ServerDownloadService _downloadService;
        private readonly JavaDownloadService _javaDownloadService;
        private readonly MainViewModel _mainViewModel;

        private int _currentStep = 1;
        private string _statusMessage = string.Empty;
        private bool _isProcessing = false;
        private bool _javaInstalled = false;
        private string _javaVersion = string.Empty;
        private string _serverName = "My Hytale Server";
        private string _serverDirectory = string.Empty;
        private string _downloadMethod = "Downloader";
        private string _launcherPath = string.Empty;
        private int _port = 5520;
        private string _bindAddress = "0.0.0.0";
        private int _maxMemoryMB = 4096;
        private int _minMemoryMB = 2048;

        public ServerSetupViewModel(MainViewModel mainViewModel)
        {
            _javaService = new JavaService();
            _downloadService = new ServerDownloadService();
            _javaDownloadService = new JavaDownloadService();
            _mainViewModel = mainViewModel;

            _downloadService.ProgressUpdate += (s, msg) => StatusMessage = msg;
            _downloadService.ProgressPercentage += (s, percent) => { };
            
            _javaDownloadService.ProgressUpdate += (s, msg) => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => StatusMessage = msg);
            };
            _javaDownloadService.ProgressPercentage += (s, percent) => { };

            CheckJavaCommand = new AsyncRelayCommand(CheckJavaAsync);
            DownloadJavaCommand = new AsyncRelayCommand(DownloadJavaAsync, () => !IsProcessing && !string.IsNullOrWhiteSpace(ServerDirectory));
            BrowseServerDirectoryCommand = new RelayCommand(BrowseServerDirectory);
            BrowseLauncherPathCommand = new RelayCommand(BrowseLauncherPath);
            NextStepCommand = new RelayCommand(NextStep, () => CanProceed);
            PreviousStepCommand = new RelayCommand(PreviousStep, () => CurrentStep > 1);
            FinishSetupCommand = new AsyncRelayCommand(FinishSetupAsync, () => CanFinish);

            // Set default server directory next to executable
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            ServerDirectory = Path.Combine(appDir, "servers", ServerName);
            
            // Update ServerDirectory when ServerName changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ServerName))
                {
                    // Only update if directory hasn't been manually changed
                    var expectedDir = Path.Combine(appDir, "servers", ServerName);
                    if (ServerDirectory == Path.Combine(appDir, "servers", _serverName) || 
                        ServerDirectory == Path.Combine(appDir, "server", _serverName))
                    {
                        ServerDirectory = expectedDir;
                    }
                }
            };

            // Set default launcher path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            LauncherPath = Path.Combine(appDataPath, "Hytale", "install", "release", "package", "game", "latest");

            // Auto-check Java on load
            _ = CheckJavaAsync();
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                OnPropertyChanged();
                PreviousStepCommand.RaiseCanExecuteChanged();
                NextStepCommand.RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
            }
        }

        public bool JavaInstalled
        {
            get => _javaInstalled;
            set
            {
                _javaInstalled = value;
                OnPropertyChanged();
            }
        }

        public string JavaVersion
        {
            get => _javaVersion;
            set
            {
                _javaVersion = value;
                OnPropertyChanged();
            }
        }

        private string _previousServerName = string.Empty;
        
        public string ServerName
        {
            get => _serverName;
            set
            {
                var oldName = _serverName;
                _serverName = value;
                OnPropertyChanged();
                FinishSetupCommand.RaiseCanExecuteChanged();
                
                // Auto-update server directory if it matches the old name pattern
                if (!string.IsNullOrEmpty(oldName) && !string.IsNullOrEmpty(ServerDirectory))
                {
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    var oldExpectedPath = Path.Combine(appDir, "servers", oldName);
                    var oldServerPath = Path.Combine(appDir, "server", oldName);
                    
                    if (ServerDirectory == oldExpectedPath || ServerDirectory == oldServerPath)
                    {
                        ServerDirectory = Path.Combine(appDir, "servers", value);
                    }
                }
            }
        }

        public string ServerDirectory
        {
            get => _serverDirectory;
            set
            {
                _serverDirectory = value;
                OnPropertyChanged();
                FinishSetupCommand.RaiseCanExecuteChanged();
                DownloadJavaCommand.RaiseCanExecuteChanged();
                
                // Store previous server name when directory is set
                if (!string.IsNullOrEmpty(value))
                {
                    var dirName = Path.GetFileName(value);
                    if (dirName == ServerName)
                    {
                        _previousServerName = ServerName;
                    }
                }
            }
        }

        public string DownloadMethod
        {
            get => _downloadMethod;
            set
            {
                _downloadMethod = value;
                OnPropertyChanged();
            }
        }

        public string LauncherPath
        {
            get => _launcherPath;
            set
            {
                _launcherPath = value;
                OnPropertyChanged();
            }
        }

        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged();
            }
        }

        public string BindAddress
        {
            get => _bindAddress;
            set
            {
                _bindAddress = value;
                OnPropertyChanged();
            }
        }

        public int MaxMemoryMB
        {
            get => _maxMemoryMB;
            set
            {
                _maxMemoryMB = value;
                OnPropertyChanged();
            }
        }

        public int MinMemoryMB
        {
            get => _minMemoryMB;
            set
            {
                _minMemoryMB = value;
                OnPropertyChanged();
            }
        }

        public ICommand CheckJavaCommand { get; }
        public ICommand DownloadJavaCommand { get; }
        public ICommand BrowseServerDirectoryCommand { get; }
        public ICommand BrowseLauncherPathCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand FinishSetupCommand { get; }

        private bool CanProceed
        {
            get
            {
                return CurrentStep switch
                {
                    1 => JavaInstalled,
                    2 => !string.IsNullOrWhiteSpace(ServerDirectory),
                    3 => true,
                    _ => false
                };
            }
        }

        private bool CanFinish => !string.IsNullOrWhiteSpace(ServerName) && !string.IsNullOrWhiteSpace(ServerDirectory);

        private async Task CheckJavaAsync()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsProcessing = true;
                StatusMessage = "Checking Java installation...";
            });

            try
            {
                var javaInfo = await _javaService.CheckJavaInstallationAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Trust the service result - it already prioritizes local java/
                    JavaInstalled = javaInfo.IsInstalled && javaInfo.IsVersion25;
                    
                    if (javaInfo.IsInstalled)
                    {
                        JavaVersion = javaInfo.Version;
                        if (javaInfo.IsVersion25)
                        {
                            StatusMessage = $"✓ Java 25 found: {javaInfo.Version}";
                            if (!string.IsNullOrEmpty(javaInfo.JavaPath) && javaInfo.JavaPath != "java")
                            {
                                StatusMessage += $" (at {javaInfo.JavaPath})";
                            }
                        }
                        else
                        {
                            // Only show "too old" if it's NOT from local java/ directory
                            var appDir = AppDomain.CurrentDomain.BaseDirectory;
                            var localJavaDir = Path.Combine(appDir, "java");
                            var isLocalJava = !string.IsNullOrEmpty(javaInfo.JavaPath) && 
                                            javaInfo.JavaPath.StartsWith(localJavaDir, StringComparison.OrdinalIgnoreCase);
                            
                            if (isLocalJava)
                            {
                                // Local Java found but version check failed - still allow it
                                StatusMessage = $"✓ Java found in local directory: {javaInfo.Version}";
                                JavaInstalled = true; // Override - trust local Java
                            }
                            else
                            {
                                StatusMessage = $"⚠ Java found but version is too old: {javaInfo.Version}. Java 25 is required.";
                            }
                        }
                    }
                    else
                    {
                        JavaVersion = "Java 25 not found";
                        StatusMessage = $"✗ Java 25 is required. You can download it automatically using the button below, or install from: {_javaService.GetJavaDownloadUrl()}";
                    }

                    IsProcessing = false;
                    NextStepCommand.RaiseCanExecuteChanged();
                    DownloadJavaCommand.RaiseCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Error checking Java: {ex.Message}";
                    JavaVersion = "Error checking Java";
                    IsProcessing = false;
                    DownloadJavaCommand.RaiseCanExecuteChanged();
                });
            }
        }

        private async Task DownloadJavaAsync()
        {
            if (string.IsNullOrWhiteSpace(ServerDirectory))
            {
                StatusMessage = "Please set a server directory first.";
                return;
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsProcessing = true;
                StatusMessage = "Starting Java download...";
            });

            try
            {
                // Determine Java installation directory (near server directory)
                var javaDir = _javaDownloadService.GetJavaDirectory(ServerDirectory);
                Directory.CreateDirectory(javaDir);

                var javaExe = await _javaDownloadService.DownloadJava25Async(javaDir);

                if (string.IsNullOrEmpty(javaExe))
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Failed to download Java or could not locate java.exe. Please check bin/java/ directory.";
                        IsProcessing = false;
                        DownloadJavaCommand.RaiseCanExecuteChanged();
                    });
                    return;
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Verifying downloaded Java installation at: {javaExe}";
                });

                // Verify the downloaded Java by checking its version
                if (!string.IsNullOrEmpty(javaExe))
                {
                    try
                    {
                        var processStartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = javaExe,
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var process = System.Diagnostics.Process.Start(processStartInfo);
                        if (process != null)
                        {
                            var output = process.StandardError.ReadToEnd();
                            process.WaitForExit();

                            if (process.ExitCode == 0)
                            {
                                // Parse version to check if it's Java 25
                                var versionMatch = System.Text.RegularExpressions.Regex.Match(output, @"(?:openjdk|java)\s+(\d+)\.(\d+)\.(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (!versionMatch.Success)
                                {
                                    versionMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\.(\d+)\.(\d+)");
                                }

                                bool isVersion25 = false;
                                if (versionMatch.Success)
                                {
                                    var majorVersion = int.Parse(versionMatch.Groups[1].Value);
                                    isVersion25 = majorVersion >= 25;
                                }

                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    JavaVersion = output.Trim();
                                    JavaInstalled = isVersion25;
                                    if (isVersion25)
                                    {
                                        StatusMessage = $"✓ Java 25 downloaded and verified: {javaExe}";
                                    }
                                    else
                                    {
                                        StatusMessage = $"⚠ Java downloaded but version may not be 25: {output.Trim()}";
                                    }
                                    IsProcessing = false;
                                    NextStepCommand.RaiseCanExecuteChanged();
                                    DownloadJavaCommand.RaiseCanExecuteChanged();
                                });
                                
                                // Re-check Java installation to update the service
                                _ = CheckJavaAsync();
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = $"Java downloaded but verification failed: {ex.Message}";
                        });
                    }
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!string.IsNullOrEmpty(javaExe))
                    {
                        StatusMessage = $"Java downloaded to: {javaExe}. Re-checking installation...";
                        // Re-check Java installation
                        _ = CheckJavaAsync();
                    }
                    else
                    {
                        StatusMessage = "Java download completed but java.exe not found. Please check the java/ directory.";
                    }
                    IsProcessing = false;
                    DownloadJavaCommand.RaiseCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Error downloading Java: {ex.Message}";
                    IsProcessing = false;
                    DownloadJavaCommand.RaiseCanExecuteChanged();
                });
            }
        }

        private void BrowseServerDirectory()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select server directory",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ServerDirectory = dialog.SelectedPath;
            }
        }

        private void BrowseLauncherPath()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Hytale launcher installation directory",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LauncherPath = dialog.SelectedPath;
            }
        }

        private void NextStep()
        {
            if (CanProceed && CurrentStep < 3)
            {
                CurrentStep++;
            }
        }

        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                CurrentStep--;
            }
        }

        private async Task FinishSetupAsync()
        {
            IsProcessing = true;
            StatusMessage = "Setting up server...";

            try
            {
                Directory.CreateDirectory(ServerDirectory);

                // Download or copy server files
                bool success = false;
                if (DownloadMethod == "Downloader")
                {
                    success = await _downloadService.DownloadServerFilesAsync(string.Empty, ServerDirectory);
                }
                else
                {
                    success = await _downloadService.CopyFromLauncherAsync(LauncherPath, ServerDirectory);
                }

                if (!success)
                {
                    StatusMessage = "Failed to download/copy server files. Please check the error messages above.";
                    IsProcessing = false;
                    return;
                }

                // Create server instance
                var javaInfo = await _javaService.CheckJavaInstallationAsync();
                
                // If Java was downloaded, use that path instead
                string javaPath = javaInfo.JavaPath;
                if (string.IsNullOrEmpty(javaPath) || javaPath == "java")
                {
                    // Check if we have a downloaded Java near the server
                    var javaDir = _javaDownloadService.GetJavaDirectory(ServerDirectory);
                    var downloadedJava = FindJavaInDirectory(javaDir);
                    if (!string.IsNullOrEmpty(downloadedJava))
                    {
                        javaPath = downloadedJava;
                    }
                }
                
                var server = new ServerInstance
                {
                    Name = ServerName,
                    ServerPath = ServerDirectory,
                    AssetsPath = Path.Combine(ServerDirectory, "Assets.zip"),
                    JavaPath = javaPath,
                    Port = Port,
                    BindAddress = BindAddress,
                    MaxMemoryMB = MaxMemoryMB,
                    MinMemoryMB = MinMemoryMB
                };

                _mainViewModel.AddServer(server);
                StatusMessage = "Server setup complete!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string? FindJavaInDirectory(string directory)
        {
            if (!Directory.Exists(directory)) return null;

            try
            {
                var dirs = Directory.GetDirectories(directory);
                foreach (var dir in dirs)
                {
                    var javaExe = Path.Combine(dir, "bin", "java.exe");
                    if (File.Exists(javaExe))
                    {
                        return javaExe;
                    }

                    // Check subdirectories
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

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

