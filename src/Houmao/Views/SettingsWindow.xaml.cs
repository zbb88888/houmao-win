using System.Windows;
using System.Windows.Input;
using Houmao.ViewModels;

namespace Houmao.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadSettings();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Esc 关闭窗口
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // 将密码框内容同步到 ViewModel
            if (_viewModel.EditingProvider != null)
            {
                _viewModel.EditingProvider.ApiKey = ApiKeyPasswordBox.Password;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}