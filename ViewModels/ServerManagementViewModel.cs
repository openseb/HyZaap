using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HyZaap.Models;
using HyZaap.Services;

namespace HyZaap.ViewModels
{
    public class ServerManagementViewModel : INotifyPropertyChanged
    {
        private readonly ServerProcessService _processService;
        private readonly ConfigService _configService;
        private readonly ServerInstance _server;

        private ObservableCollection<string> _consoleOutput = new();
        private string _commandInput = string.Empty;
        private EasyConfigEditorViewModel? _easyConfig;
        private bool _profileSelectionSent = false;

        public ServerManagementViewModel(ServerInstance server)
        {
            _server = server;
            _originalServerName = server.Name;
            _processService = new ServerProcessService();
            _configService = new ConfigService();
            _easyConfig = new EasyConfigEditorViewModel(server);

            // If server is running but we don't have the process, try to reattach
            if (server.IsRunning && server.ProcessId.HasValue && server.Process == null)
            {
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(server.ProcessId.Value);
                    if (!process.HasExited)
                    {
                        server.Process = process;
                        // Note: We can't reattach to output streams, but we can control the process
                    }
                    else
                    {
                        // Process has exited
                        server.IsRunning = false;
                        server.ProcessId = null;
                        _configService.SaveServer(server);
                    }
                }
                catch
                {
                    // Process doesn't exist
                    server.IsRunning = false;
                    server.ProcessId = null;
                    _configService.SaveServer(server);
                }
            }

            _processService.OutputReceived += (s, msg) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    // Reset cleared flag when new output arrives
                    if (_server.ConsoleCleared)
                    {
                        _server.ConsoleCleared = false;
                    }
                    ConsoleOutput.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
                    
                    // Check for authentication URL in the message
                    CheckForAuthUrl(msg);
                    
                    // Check for profile selection prompt
                    CheckForProfileSelection(msg);
                });
            };

            _processService.ProcessExited += (s, e) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ConsoleOutput.Add($"[{DateTime.Now:HH:mm:ss}] Server stopped.");
                    IsRunning = false;
                });
            };

            StartServerCommand = new RelayCommand(async () => await StartServerAsync(), () => !IsRunning);
            StopServerCommand = new RelayCommand(async () => await StopServerAsync(), () => IsRunning);
            RestartServerCommand = new RelayCommand(async () => await RestartServerAsync(), () => IsRunning);
            SendCommandCommand = new RelayCommand(async () => await SendCommandAsync(), () => IsRunning && !string.IsNullOrWhiteSpace(CommandInput));
            SaveConfigCommand = new RelayCommand(SaveConfig);
            ClearConsoleCommand = new RelayCommand(ClearConsole);
            DeleteServerCommand = new RelayCommand(DeleteServer, () => !IsRunning);
            AuthLoginCommand = new AsyncRelayCommand(async () => await AuthLoginAsync(), () => IsRunning);

            // Subscribe to collection changes for auto-scroll
            ConsoleOutput.CollectionChanged += (s, e) => ConsoleOutputChanged?.Invoke();

            // Load existing console output
            LoadConsoleOutput();
        }

        private string _originalServerName = string.Empty;
        
        public ServerInstance Server => _server;
        
        public string ServerName
        {
            get => _server.Name;
            set
            {
                if (_server.Name != value)
                {
                    var oldName = _server.Name;
                    _server.Name = value;
                    OnPropertyChanged();
                    
                    // Rename server folder if it matches the old name pattern
                    if (!string.IsNullOrEmpty(_originalServerName) && !string.IsNullOrEmpty(_server.ServerPath))
                    {
                        var parentDir = Path.GetDirectoryName(_server.ServerPath);
                        if (!string.IsNullOrEmpty(parentDir))
                        {
                            var oldExpectedPath = Path.Combine(parentDir, _originalServerName);
                            if (_server.ServerPath == oldExpectedPath || Path.GetFileName(_server.ServerPath) == _originalServerName)
                            {
                                var newPath = Path.Combine(parentDir, value);
                                try
                                {
                                    if (Directory.Exists(_server.ServerPath) && !Directory.Exists(newPath))
                                    {
                                        Directory.Move(_server.ServerPath, newPath);
                                        _server.ServerPath = newPath;
                                        _configService.SaveServer(_server);
                                    }
                                }
                                catch
                                {
                                    // If rename fails, just update the path in config
                                    _server.ServerPath = newPath;
                                    _configService.SaveServer(_server);
                                }
                            }
                        }
                    }
                    _originalServerName = value;
                    _configService.SaveServer(_server);
                }
            }
        }

        public bool IsRunning
        {
            get => _server.IsRunning;
            set
            {
                _server.IsRunning = value;
                OnPropertyChanged();
                StartServerCommand.RaiseCanExecuteChanged();
                StopServerCommand.RaiseCanExecuteChanged();
                RestartServerCommand.RaiseCanExecuteChanged();
                SendCommandCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<string> ConsoleOutput
        {
            get => _consoleOutput;
            set
            {
                _consoleOutput = value;
                OnPropertyChanged();
            }
        }

        public string CommandInput
        {
            get => _commandInput;
            set
            {
                _commandInput = value;
                OnPropertyChanged();
                SendCommandCommand.RaiseCanExecuteChanged();
            }
        }

        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand RestartServerCommand { get; }
        public ICommand SendCommandCommand { get; }
        public ICommand SaveConfigCommand { get; }
        public ICommand ClearConsoleCommand { get; }
        public ICommand DeleteServerCommand { get; }
        public ICommand AuthLoginCommand { get; }
        
        private string? _authUrl = null;
        
        public string? AuthUrl
        {
            get => _authUrl;
            set
            {
                _authUrl = value;
                OnPropertyChanged();
            }
        }

        public EasyConfigEditorViewModel EasyConfig
        {
            get => _easyConfig ??= new EasyConfigEditorViewModel(_server);
        }

        public event Action? ConsoleOutputChanged;
        public event Action? ServerDeleted;

        private async Task StartServerAsync()
        {
            var success = await _processService.StartServerAsync(_server);
            if (success)
            {
                IsRunning = true;
                ConsoleOutput.Add($"[{DateTime.Now:HH:mm:ss}] Server starting...");
                _configService.SaveServer(_server);
            }
        }

        private async Task StopServerAsync()
        {
            var success = await _processService.StopServerAsync(_server);
            if (success)
            {
                IsRunning = false;
                ConsoleOutput.Add($"[{DateTime.Now:HH:mm:ss}] Server stopping...");
                _configService.SaveServer(_server);
            }
        }

        private async Task RestartServerAsync()
        {
            await StopServerAsync();
            await Task.Delay(2000);
            await StartServerAsync();
        }

        private async Task SendCommandAsync()
        {
            if (string.IsNullOrWhiteSpace(CommandInput)) return;

            var command = CommandInput;
            CommandInput = string.Empty;

            var success = await _processService.SendCommandAsync(_server, command);
            if (success)
            {
                ConsoleOutput.Add($"[{DateTime.Now:HH:mm:ss}] > {command}");
            }
        }

        private void SaveConfig()
        {
            _configService.SaveServer(_server);
        }

        private void ClearConsole()
        {
            ConsoleOutput.Clear();
            _server.ConsoleCleared = true;
            _configService.SaveServer(_server);
        }

        private void DeleteServer()
        {
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete '{_server.Name}'?\n\nThis will remove the server from the list, but the server files will remain on disk.",
                "Delete Server",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _configService.DeleteServer(_server.Id);
                ServerDeleted?.Invoke();
            }
        }

        private async Task AuthLoginAsync()
        {
            AuthUrl = null;
            _profileSelectionSent = false; // Reset flag for new auth session
            await _processService.SendCommandAsync(_server, "/auth login device");
            
            // Wait a bit for the response, then check console output for URL
            // The server may take a moment to respond
            await Task.Delay(2000);
            
            // Check existing console output for auth URL
            // Check in reverse order to get the most recent auth attempt
            for (int i = ConsoleOutput.Count - 1; i >= 0; i--)
            {
                CheckForAuthUrl(ConsoleOutput[i]);
                if (!string.IsNullOrEmpty(AuthUrl))
                {
                    break; // Found the URL, stop searching
                }
            }
            
            // Also monitor for a few more seconds in case the response is delayed
            var startTime = DateTime.Now;
            while (string.IsNullOrEmpty(AuthUrl) && (DateTime.Now - startTime).TotalSeconds < 5)
            {
                await Task.Delay(500);
                // Check the latest console output
                if (ConsoleOutput.Count > 0)
                {
                    CheckForAuthUrl(ConsoleOutput[ConsoleOutput.Count - 1]);
                }
            }
            
            // After URL is found, monitor for profile selection prompt
            // This can happen after the user completes authentication in the browser
            startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < 30) // Wait up to 30 seconds for profile selection
            {
                await Task.Delay(1000);
                // Check recent console output for profile selection prompt
                if (ConsoleOutput.Count > 0)
                {
                    for (int i = Math.Max(0, ConsoleOutput.Count - 10); i < ConsoleOutput.Count; i++)
                    {
                        if (CheckForProfileSelection(ConsoleOutput[i]))
                        {
                            return; // Profile selected, we're done
                        }
                    }
                }
            }
        }

        private bool CheckForProfileSelection(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _profileSelectionSent) return false;

            // Strip ANSI escape codes
            var cleanMessage = System.Text.RegularExpressions.Regex.Replace(message, @"\[[0-9;]*m", "");
            
            // Check if this is a profile selection prompt
            if (cleanMessage.Contains("Multiple profiles available", StringComparison.OrdinalIgnoreCase) ||
                cleanMessage.Contains("Use '/auth select", StringComparison.OrdinalIgnoreCase))
            {
                // Look for profile lines like "[1] zfae" or "[2] PhantomRoot303"
                // We'll auto-select the first profile (profile #1)
                _profileSelectionSent = true; // Prevent duplicate sends
                Task.Run(async () =>
                {
                    await Task.Delay(1000); // Give a moment for all profile lines to appear
                    await _processService.SendCommandAsync(_server, "/auth select 1");
                });
                return true;
            }
            
            // Also check for individual profile lines and auto-select first one
            var profileMatch = System.Text.RegularExpressions.Regex.Match(cleanMessage, @"\[1\]\s+([^\s(]+)");
            if (profileMatch.Success && !_profileSelectionSent)
            {
                // Found profile #1, auto-select it
                _profileSelectionSent = true; // Prevent duplicate sends
                Task.Run(async () =>
                {
                    await Task.Delay(500); // Small delay to ensure prompt is complete
                    await _processService.SendCommandAsync(_server, "/auth select 1");
                });
                return true;
            }
            
            return false;
        }

        private void CheckForAuthUrl(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // First, strip ANSI escape codes from the message
            var cleanMessage = System.Text.RegularExpressions.Regex.Replace(message, @"\[[0-9;]*m", "");
            
            // Look for "Visit:" or "Or visit:" patterns
            var visitIndex = cleanMessage.IndexOf("Visit:", StringComparison.OrdinalIgnoreCase);
            var orVisitIndex = cleanMessage.IndexOf("Or visit:", StringComparison.OrdinalIgnoreCase);
            
            string? url = null;
            string? userCode = null;
            
            // First, try to extract the user code from "Enter code:" line
            var codeMatch = System.Text.RegularExpressions.Regex.Match(cleanMessage, @"Enter code:\s+([A-Za-z0-9]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (codeMatch.Success)
            {
                userCode = codeMatch.Groups[1].Value;
            }
            
            if (orVisitIndex >= 0)
            {
                // Prefer "Or visit:" URL as it includes the user code
                var startIndex = orVisitIndex + "Or visit:".Length;
                var urlPart = cleanMessage.Substring(startIndex).Trim();
                // Extract full URL including query parameters - match until whitespace or end of string
                var urlMatch = System.Text.RegularExpressions.Regex.Match(urlPart, @"(https?://[^\s]+)");
                if (urlMatch.Success)
                {
                    url = urlMatch.Groups[1].Value.Trim();
                    
                    // If URL doesn't have user_code parameter but we found the code, add it
                    if (!url.Contains("user_code=") && !string.IsNullOrEmpty(userCode))
                    {
                        url = $"{url}?user_code={userCode}";
                    }
                }
            }
            else if (visitIndex >= 0)
            {
                var startIndex = visitIndex + "Visit:".Length;
                var urlPart = cleanMessage.Substring(startIndex).Trim();
                // Extract base URL
                var urlMatch = System.Text.RegularExpressions.Regex.Match(urlPart, @"(https?://[^\s]+)");
                if (urlMatch.Success)
                {
                    url = urlMatch.Groups[1].Value.Trim();
                    
                    // If we only got the base URL, add user_code if we found it
                    if (!url.Contains("user_code=") && !string.IsNullOrEmpty(userCode))
                    {
                        url = $"{url}?user_code={userCode}";
                    }
                    // If still no user_code, try to find it from other console lines
                    else if (!url.Contains("user_code="))
                    {
                        // Look for "Enter code:" in recent console output (check in reverse for most recent)
                        for (int i = ConsoleOutput.Count - 1; i >= 0; i--)
                        {
                            var line = ConsoleOutput[i];
                            var cleanLine = System.Text.RegularExpressions.Regex.Replace(line, @"\[[0-9;]*m", "");
                            var codeMatch2 = System.Text.RegularExpressions.Regex.Match(cleanLine, @"Enter code:\s+([A-Za-z0-9]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (codeMatch2.Success)
                            {
                                userCode = codeMatch2.Groups[1].Value;
                                url = $"{url}?user_code={userCode}";
                                break;
                            }
                        }
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(url) && url != AuthUrl)
            {
                // Final cleanup - ensure no trailing characters
                url = url.Trim();
                
                AuthUrl = url;
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // If opening fails, at least we have the URL stored
                }
            }
        }

        private void LoadConsoleOutput()
        {
            // Don't reload logs if console was cleared
            if (_server.ConsoleCleared)
            {
                return;
            }

            var logsPath = Path.Combine(_server.ServerPath, "Server", "logs");
            if (Directory.Exists(logsPath))
            {
                var logFiles = Directory.GetFiles(logsPath, "*.log");
                if (logFiles.Length > 0)
                {
                    var latestLog = new FileInfo(logFiles[0]);
                    foreach (var file in logFiles)
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTime > latestLog.LastWriteTime)
                        {
                            latestLog = info;
                        }
                    }

                    try
                    {
                        var lines = File.ReadAllLines(latestLog.FullName);
                        // Only load recent lines to avoid overwhelming the console
                        var startIndex = Math.Max(0, lines.Length - 500); // Last 500 lines
                        for (int i = startIndex; i < lines.Length; i++)
                        {
                            ConsoleOutput.Add(lines[i]);
                        }
                    }
                    catch { }
                }
            }
            
            // If server is running but we reattached, add a note
            if (_server.IsRunning && _server.Process != null)
            {
                try
                {
                    // Check if we can actually read from the process
                    var test = _server.Process.StandardInput;
                }
                catch
                {
                    // Can't use StandardInput - add warning message
                    ConsoleOutput.Add($"[{DateTime.Now:HH:mm:ss}] ⚠ Server was started by another instance. Console output and commands may be limited.");
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

