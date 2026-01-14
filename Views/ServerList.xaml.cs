using System.Windows;
using System.Windows.Input;
using HyZaap.ViewModels;

namespace HyZaap.Views
{
    public partial class ServerList : System.Windows.Controls.UserControl
    {
        public ServerList()
        {
            InitializeComponent();
        }

        private void ServerCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && sender is FrameworkElement element)
            {
                if (element.DataContext is Models.ServerInstance server)
                {
                    viewModel.SelectedServer = server;
                    viewModel.NavigateToServerManagementCommand.Execute(null);
                }
            }
        }

        private void EditConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && sender is FrameworkElement element)
            {
                if (element.DataContext is Models.ServerInstance server)
                {
                    viewModel.SelectedServer = server;
                    viewModel.EditServerCommand.Execute(null);
                }
            }
            e.Handled = true;
        }
    }
}

