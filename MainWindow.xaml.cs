using System.Windows;
using System.Windows.Controls;
using HyZaap.ViewModels;
using HyZaap.Views;

namespace HyZaap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            // Set initial view based on current view property
            UpdateView();
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentView))
                {
                    UpdateView();
                }
            };
        }

        private void UpdateView()
        {
            System.Windows.Controls.UserControl? content = null;

            switch (_viewModel.CurrentView)
            {
                case "ServerSetup":
                    content = new ServerSetup { DataContext = new ServerSetupViewModel(_viewModel) };
                    break;
                case "ServerManagement":
                    if (_viewModel.SelectedServer != null)
                    {
                        content = new ServerManagement { DataContext = new ServerManagementViewModel(_viewModel.SelectedServer) };
                    }
                    break;
                case "ServerConfigEditor":
                    if (_viewModel.SelectedServer != null)
                    {
                        var editor = new ServerConfigEditor { DataContext = new ServerConfigEditorViewModel(_viewModel.SelectedServer) };
                        if (editor.DataContext is ServerConfigEditorViewModel editorViewModel)
                        {
                            editorViewModel.NavigateBack = () => _viewModel.CurrentView = "ServerList";
                        }
                        content = editor;
                    }
                    break;
                default:
                    content = new ServerList { DataContext = _viewModel };
                    break;
            }

            ContentArea.Content = content ?? new ServerList { DataContext = _viewModel };
        }
    }
}