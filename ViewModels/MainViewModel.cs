using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HyZaap.Models;
using HyZaap.Services;

namespace HyZaap.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ConfigService _configService;
        private ServerInstance? _selectedServer;
        private string _currentView = "ServerList";

        public MainViewModel()
        {
            _configService = new ConfigService();
            var servers = _configService.LoadServers();
            
            // Check if processes are still running
            var processService = new ServerProcessService();
            foreach (var server in servers)
            {
                if (server.IsRunning)
                {
                    var isRunning = processService.CheckProcessRunning(server);
                    if (!isRunning)
                    {
                        server.IsRunning = false;
                        server.ProcessId = null;
                        _configService.SaveServer(server);
                    }
                }
            }
            
            Servers = new ObservableCollection<ServerInstance>(servers);
            
            NavigateToSetupCommand = new RelayCommand(() => CurrentView = "ServerSetup");
            NavigateToServerListCommand = new RelayCommand(() => CurrentView = "ServerList");
            NavigateToServerManagementCommand = new RelayCommand(() => CurrentView = "ServerManagement", () => SelectedServer != null);
            DeleteServerCommand = new RelayCommand(DeleteServer, () => SelectedServer != null);
            EditServerCommand = new RelayCommand(EditServer, () => SelectedServer != null);
        }

        public ObservableCollection<ServerInstance> Servers { get; }

        public ServerInstance? SelectedServer
        {
            get => _selectedServer;
            set
            {
                _selectedServer = value;
                OnPropertyChanged();
                NavigateToServerManagementCommand.RaiseCanExecuteChanged();
                DeleteServerCommand.RaiseCanExecuteChanged();
                EditServerCommand.RaiseCanExecuteChanged();
            }
        }

        public string CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateToSetupCommand { get; }
        public ICommand NavigateToServerListCommand { get; }
        public ICommand NavigateToServerManagementCommand { get; }
        public ICommand DeleteServerCommand { get; }
        public ICommand EditServerCommand { get; }

        public void RefreshServers()
        {
            Servers.Clear();
            foreach (var server in _configService.LoadServers())
            {
                Servers.Add(server);
            }
        }

        public void AddServer(ServerInstance server)
        {
            _configService.SaveServer(server);
            Servers.Add(server);
            SelectedServer = server;
            CurrentView = "ServerManagement";
        }

        private void DeleteServer()
        {
            if (SelectedServer == null) return;

            _configService.DeleteServer(SelectedServer.Id);
            Servers.Remove(SelectedServer);
            SelectedServer = null;
            CurrentView = "ServerList";
        }

        private void EditServer()
        {
            if (SelectedServer == null) return;
            CurrentView = "ServerConfigEditor";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
        {
            try
            {
                _execute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing command: {ex}");
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing async command: {ex}");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public static class CommandExtensions
    {
        public static void RaiseCanExecuteChanged(this ICommand command)
        {
            if (command is RelayCommand relayCmd)
                relayCmd.RaiseCanExecuteChanged();
            else if (command is AsyncRelayCommand asyncCmd)
                asyncCmd.RaiseCanExecuteChanged();
        }
    }
}

