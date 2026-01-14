using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using HyZaap.Models;
using HyZaap.Services;

namespace HyZaap.ViewModels
{
    public class ServerConfigEditorViewModel : INotifyPropertyChanged
    {
        private readonly ServerInstance _server;
        private readonly ConfigService _configService;
        private readonly ServerProcessService _processService;
        private string _configJson = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isProcessing = false;

        public ServerConfigEditorViewModel(ServerInstance server)
        {
            _server = server;
            _configService = new ConfigService();
            _processService = new ServerProcessService();

            SaveCommand = new AsyncRelayCommand(async () => await SaveConfigAsync(), () => !IsProcessing);
            SaveAndRestartCommand = new AsyncRelayCommand(async () => await SaveAndRestartAsync(), () => !IsProcessing);
            CancelCommand = new RelayCommand(() => NavigateBack?.Invoke());

            LoadConfig();
        }

        public ServerInstance Server => _server;

        public string ConfigJson
        {
            get => _configJson;
            set
            {
                _configJson = value;
                OnPropertyChanged();
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
                SaveCommand.RaiseCanExecuteChanged();
                SaveAndRestartCommand.RaiseCanExecuteChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand SaveAndRestartCommand { get; }
        public ICommand CancelCommand { get; }

        public Action? NavigateBack { get; set; }

        private void LoadConfig()
        {
            try
            {
                var configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    // Format JSON for better readability
                    var doc = JsonDocument.Parse(json);
                    ConfigJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    StatusMessage = "Config loaded successfully.";
                }
                else
                {
                    // Create default config
                    ConfigJson = GetDefaultConfig();
                    StatusMessage = "No config file found. Using default template.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading config: {ex.Message}";
                ConfigJson = GetDefaultConfig();
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
                    // Validate JSON
                    JsonDocument.Parse(ConfigJson);

                    var configPath = GetConfigPath();
                    var configDir = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(configDir))
                    {
                        Directory.CreateDirectory(configDir);
                    }

                    File.WriteAllText(configPath, ConfigJson);
                });

                StatusMessage = "Config saved successfully!";
            }
            catch (JsonException ex)
            {
                StatusMessage = $"Invalid JSON: {ex.Message}";
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
                
                // Navigate back to management view
                await Task.Delay(1000);
                NavigateBack?.Invoke();
            }
        }

        private string GetConfigPath()
        {
            // Try to find the config.json in the universe/worlds/default directory
            var defaultPath = Path.Combine(_server.ServerPath, "Server", "universe", "worlds", "default", "config.json");
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }

            // Fallback to Server/config.json
            return Path.Combine(_server.ServerPath, "Server", "config.json");
        }

        private string GetDefaultConfig()
        {
            return @"{
  ""Version"": 4,
  ""UUID"": {
    ""$binary"": """",
    ""$type"": ""04""
  },
  ""Seed"": 0,
  ""WorldGen"": {
    ""Type"": ""Hytale"",
    ""Name"": ""Default""
  },
  ""WorldMap"": {
    ""Type"": ""WorldGen""
  },
  ""ChunkStorage"": {
    ""Type"": ""Hytale""
  },
  ""ChunkConfig"": {},
  ""IsTicking"": true,
  ""IsBlockTicking"": true,
  ""IsPvpEnabled"": false,
  ""IsFallDamageEnabled"": true,
  ""IsGameTimePaused"": false,
  ""GameTime"": ""0001-01-01T00:00:00Z"",
  ""RequiredPlugins"": {},
  ""IsSpawningNPC"": true,
  ""IsSpawnMarkersEnabled"": true,
  ""IsAllNPCFrozen"": false,
  ""GameplayConfig"": ""Default"",
  ""IsCompassUpdating"": true,
  ""IsSavingPlayers"": true,
  ""IsSavingChunks"": true,
  ""IsUnloadingChunks"": true,
  ""IsObjectiveMarkersEnabled"": true,
  ""DeleteOnUniverseStart"": false,
  ""DeleteOnRemove"": false,
  ""ResourceStorage"": {
    ""Type"": ""Hytale""
  },
  ""Plugin"": {}
}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

