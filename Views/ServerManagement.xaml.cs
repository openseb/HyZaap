using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using HyZaap.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace HyZaap.Views
{
    public partial class ServerManagement : System.Windows.Controls.UserControl
    {
        public ServerManagement()
        {
            InitializeComponent();
            Loaded += ServerManagement_Loaded;
        }

        private void ServerManagement_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ServerManagementViewModel viewModel)
            {
                viewModel.ConsoleOutputChanged += ScrollToBottom;
                viewModel.ServerDeleted += NavigateBack;
                
                // Set initial auth mode selection
                if (AuthModeComboBox != null && viewModel.Server != null)
                {
                    foreach (System.Windows.Controls.ComboBoxItem item in AuthModeComboBox.Items)
                    {
                        if (item.Tag?.ToString() == viewModel.Server.AuthMode)
                        {
                            AuthModeComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void NavigateBack()
        {
            if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.RefreshServers();
                mainViewModel.CurrentView = "ServerList";
            }
        }

        private void AuthModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ServerManagementViewModel viewModel && 
                sender is System.Windows.Controls.ComboBox comboBox && 
                comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                viewModel.Server.AuthMode = selectedItem.Tag?.ToString() ?? "authenticated";
            }
        }

        private void ScrollToBottom()
        {
            if (ConsoleScrollViewer != null)
            {
                ConsoleScrollViewer.ScrollToEnd();
            }
        }

        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ServerManagementViewModel viewModel)
            {
                viewModel.SendCommandCommand.Execute(null);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.CurrentView = "ServerList";
            }
        }
    }
}

