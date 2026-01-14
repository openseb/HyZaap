using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Input;
using HyZaap.Models;
using HyZaap.Services;

namespace HyZaap.ViewModels
{
    public class EasyConfigEditorViewModel : INotifyPropertyChanged
    {
        private readonly ServerInstance _server;
        private readonly ConfigService _configService;
        private readonly ServerProcessService _processService;
        private string _statusMessage = string.Empty;
        private bool _isProcessing = false;
        private JsonObject? _configData;

        // Easy config properties
        private bool _isPvpEnabled = false;
        private bool _isFallDamageEnabled = true;
        private bool _isTicking = true;
        private bool _isBlockTicking = true;
        private bool _isSpawningNPC = true;
        private bool _isSavingPlayers = true;
        private bool _isSavingChunks = true;
        private bool _isUnloadingChunks = true;
        private long _seed = 0;

        public EasyConfigEditorViewModel(ServerInstance server)
        {
            _server = server;
            _configService = new ConfigService();
            _processService = new ServerProcessService();

            SaveCommand = new AsyncRelayCommand(async () => await SaveConfigAsync(), () => !IsProcessing);
            SaveAndRestartCommand = new AsyncRelayCommand(async () => await SaveAndRestartAsync(), () => !IsProcessing);

            LoadConfig();
        }

        public ServerInstance Server => _server;

        public bool IsPvpEnabled
        {
            get => _isPvpEnabled;
            set { _isPvpEnabled = value; OnPropertyChanged(); }
        }

        public bool IsFallDamageEnabled
        {
            get => _isFallDamageEnabled;
            set { _isFallDamageEnabled = value; OnPropertyChanged(); }
        }

        public bool IsTicking
        {
            get => _isTicking;
            set { _isTicking = value; OnPropertyChanged(); }
        }

        public bool IsBlockTicking
        {
            get => _isBlockTicking;
            set { _isBlockTicking = value; OnPropertyChanged(); }
        }

        public bool IsSpawningNPC
        {
            get => _isSpawningNPC;
            set { _isSpawningNPC = value; OnPropertyChanged(); }
        }

        public bool IsSavingPlayers
        {
            get => _isSavingPlayers;
            set { _isSavingPlayers = value; OnPropertyChanged(); }
        }

        public bool IsSavingChunks
        {
            get => _isSavingChunks;
            set { _isSavingChunks = value; OnPropertyChanged(); }
        }

        public bool IsUnloadingChunks
        {
            get => _isUnloadingChunks;
            set { _isUnloadingChunks = value; OnPropertyChanged(); }
        }

        public long Seed
        {
            get => _seed;
            set { _seed = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                SaveCommand.RaiseCanExecuteChanged();
                SaveAndRestartCommand.RaiseCanExecuteChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand SaveAndRestartCommand { get; }

        private void LoadConfig()
        {
            try
            {
                var configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    _configData = JsonNode.Parse(json)?.AsObject();

                    if (_configData != null)
                    {
                        IsPvpEnabled = _configData["IsPvpEnabled"]?.GetValue<bool>() ?? false;
                        IsFallDamageEnabled = _configData["IsFallDamageEnabled"]?.GetValue<bool>() ?? true;
                        IsTicking = _configData["IsTicking"]?.GetValue<bool>() ?? true;
                        IsBlockTicking = _configData["IsBlockTicking"]?.GetValue<bool>() ?? true;
                        IsSpawningNPC = _configData["IsSpawningNPC"]?.GetValue<bool>() ?? true;
                        IsSavingPlayers = _configData["IsSavingPlayers"]?.GetValue<bool>() ?? true;
                        IsSavingChunks = _configData["IsSavingChunks"]?.GetValue<bool>() ?? true;
                        IsUnloadingChunks = _configData["IsUnloadingChunks"]?.GetValue<bool>() ?? true;
                        Seed = _configData["Seed"]?.GetValue<long>() ?? 0;
                    }

                    StatusMessage = "Config loaded successfully.";
                }
                else
                {
                    StatusMessage = "No config file found. Using defaults.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading config: {ex.Message}";
            }
        }

        private async Task SaveConfigAsync()
        {
            IsProcessing = true;
            StatusMessage = "Saving config...";

            try
            {
                await Task.Run(() =>
                {
                    var configPath = GetConfigPath();
                    var configDir = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(configDir))
                    {
                        Directory.CreateDirectory(configDir);
                    }

                    if (_configData == null)
                    {
                        _configData = JsonNode.Parse("{}")?.AsObject() ?? new JsonObject();
                    }

                    // Update values
                    _configData["IsPvpEnabled"] = IsPvpEnabled;
                    _configData["IsFallDamageEnabled"] = IsFallDamageEnabled;
                    _configData["IsTicking"] = IsTicking;
                    _configData["IsBlockTicking"] = IsBlockTicking;
                    _configData["IsSpawningNPC"] = IsSpawningNPC;
                    _configData["IsSavingPlayers"] = IsSavingPlayers;
                    _configData["IsSavingChunks"] = IsSavingChunks;
                    _configData["IsUnloadingChunks"] = IsUnloadingChunks;
                    _configData["Seed"] = Seed;

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = _configData.ToJsonString(options);
                    File.WriteAllText(configPath, json);
                });

                StatusMessage = "Config saved successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving config: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task SaveAndRestartAsync()
        {
            await SaveConfigAsync();

            if (StatusMessage.Contains("successfully"))
            {
                StatusMessage = "Restarting server...";

                if (_server.IsRunning)
                {
                    await _processService.StopServerAsync(_server);
                    await Task.Delay(2000);
                }

                await _processService.StartServerAsync(_server);
                StatusMessage = "Config saved and server restarted!";
            }
        }

        private string GetConfigPath()
        {
            var defaultPath = Path.Combine(_server.ServerPath, "Server", "universe", "worlds", "default", "config.json");
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }
            return Path.Combine(_server.ServerPath, "Server", "config.json");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

