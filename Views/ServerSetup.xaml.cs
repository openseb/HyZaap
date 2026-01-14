using System.Windows;
using System.Windows.Controls;
using HyZaap.ViewModels;
using ComboBox = System.Windows.Controls.ComboBox;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace HyZaap.Views
{
    public partial class ServerSetup : System.Windows.Controls.UserControl
    {
        public ServerSetup()
        {
            InitializeComponent();
        }

        private void DownloadMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ServerSetupViewModel viewModel && sender is ComboBox comboBox)
            {
                if (comboBox.SelectedItem is ComboBoxItem item)
                {
                    viewModel.DownloadMethod = item.Content.ToString() == "Downloader (Recommended)" 
                        ? "Downloader" 
                        : "Copy from Launcher";
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.CurrentView = "ServerList";
            }
        }
    }
}

