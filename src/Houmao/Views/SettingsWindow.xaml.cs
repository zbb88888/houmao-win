using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Houmao.Interop;
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
            // 应用毛玻璃效果和圆角
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            
            // 检测 Windows 版本
            if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000)
            {
                // Windows 11
                DwmApi.SetAcrylic(hwnd);
                DwmApi.SetRoundCorners(hwnd);
            }
            else if (Environment.OSVersion.Version.Major >= 10)
            {
                // Windows 10
                DwmApi.SetAcrylicWin10(hwnd, 0x99FFFFFF);
            }
            
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
        
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ProviderItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var actionsPanel = FindChild<StackPanel>(border, "ActionsPanel");
                if (actionsPanel != null)
                {
                    actionsPanel.Visibility = Visibility.Visible;
                }
                
                // 如果不是默认项，添加悬停背景
                var dataContext = border.DataContext as Models.Provider;
                if (dataContext != null && !dataContext.IsDefault)
                {
                    border.Background = (Brush)FindResource("Surface");
                }
            }
        }

        private void ProviderItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var actionsPanel = FindChild<StackPanel>(border, "ActionsPanel");
                if (actionsPanel != null)
                {
                    actionsPanel.Visibility = Visibility.Collapsed;
                }
                
                // 恢复背景
                var dataContext = border.DataContext as Models.Provider;
                if (dataContext != null && !dataContext.IsDefault)
                {
                    border.Background = Brushes.Transparent;
                }
            }
        }

        private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        return typedChild;
                    }
                }

                var result = FindChild<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}