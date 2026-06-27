using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Houmao.Models;
using Houmao.ViewModels;
using Houmao.Services;
using Houmao.Interop;
using WpfInput = System.Windows.Input;

namespace Houmao.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly HotKeyManager _hotKeyManager;
        private readonly IServiceProvider _services;

        public MainWindow(MainViewModel viewModel, HotKeyManager hotKeyManager, IServiceProvider services)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _hotKeyManager = hotKeyManager;
            _services = services;
            
            DataContext = _viewModel;
            
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.InputText))
                {
                    UpdateSendButtonVisibility();
                }
            };
            
            var pasteBinding = new CommandBinding(ApplicationCommands.Paste, OnPaste, CanPaste);
            InputTextBox.CommandBindings.Add(pasteBinding);
            
            _hotKeyManager.DoubleAltPressed += HotKeyManager_DoubleAltPressed;
            
            CenterOnScreen();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InputTextBox.Focus();
            WpfInput.Keyboard.Focus(InputTextBox);
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                _viewModel.ClearConversation();
                InputTextBox.Focus();
                WpfInput.Keyboard.Focus(InputTextBox);
            }
        }

        private void Window_KeyDown(object sender, WpfInput.KeyEventArgs e)
        {
            if (e.Key == WpfInput.Key.W && WpfInput.Keyboard.Modifiers == WpfInput.ModifierKeys.Control)
            {
                Hide();
                e.Handled = true;
            }
            
            if (e.Key == WpfInput.Key.OemComma && WpfInput.Keyboard.Modifiers == WpfInput.ModifierKeys.Control)
            {
                OpenSettings();
                e.Handled = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void InputTextBox_KeyDown(object sender, WpfInput.KeyEventArgs e)
        {
            if (e.Key == WpfInput.Key.V && WpfInput.Keyboard.Modifiers == WpfInput.ModifierKeys.Control)
            {
                if (TryPasteImageFromClipboard())
                {
                    e.Handled = true;
                    return;
                }
            }
            
            if (e.Key == WpfInput.Key.Up)
            {
                var previous = _viewModel.GetPreviousCommand();
                if (previous != null)
                {
                    InputTextBox.Text = previous;
                    InputTextBox.CaretIndex = InputTextBox.Text.Length;
                }
                e.Handled = true;
            }
            else if (e.Key == WpfInput.Key.Down)
            {
                var next = _viewModel.GetNextCommand();
                InputTextBox.Text = next ?? string.Empty;
                InputTextBox.CaretIndex = InputTextBox.Text.Length;
                e.Handled = true;
            }
            
            if (e.Key == WpfInput.Key.Enter && !InputTextBox.AcceptsReturn)
            {
                _viewModel.SubmitCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AttachFileCommand.Execute(null);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SubmitCommand.Execute(null);
        }

        private void UpdateSendButtonVisibility()
        {
            SendButton.Visibility = string.IsNullOrWhiteSpace(_viewModel.InputText) 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }
        
        private void OnPaste(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    var bitmap = Clipboard.GetImage();
                    if (bitmap != null)
                    {
                        var attachment = Attachment.FromBitmapSource(bitmap);
                        if (attachment != null)
                        {
                            _viewModel.Attachments.Add(attachment);
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
            catch
            {
            }
        }
        
        private void CanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private bool TryPasteImageFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                    return false;
                
                var bitmap = Clipboard.GetImage();
                if (bitmap == null)
                    return false;
                
                var attachment = Attachment.FromBitmapSource(bitmap);
                if (attachment == null)
                    return false;
                
                _viewModel.Attachments.Add(attachment);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void HotKeyManager_DoubleAltPressed(object? sender, EventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var logPath = Path.Combine(Path.GetTempPath(), "houmao_hotkey.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Toggle! IsVisible={IsVisible}, Left={Left}, Top={Top}\n");
                    
                    if (IsVisible)
                    {
                        Hide();
                        File.AppendAllText(logPath, $"[{DateTime.Now}] Hidden\n");
                    }
                    else
                    {
                        ShowAndActivate();
                        File.AppendAllText(logPath, $"[{DateTime.Now}] Shown\n");
                    }
                });
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(Path.GetTempPath(), "houmao_hotkey.log");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Error: {ex}\n");
            }
        }

        public void ShowAndActivate()
        {
            CenterOnScreen();
            Show();
            Activate();
            InputTextBox.Focus();
            WpfInput.Keyboard.Focus(InputTextBox);
        }

        private void CenterOnScreen()
        {
            var mousePos = System.Windows.Forms.Cursor.Position;
            var screen = System.Windows.Forms.Screen.FromPoint(mousePos);
            var workingArea = screen.WorkingArea;
            
            var source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                var transform = source.CompositionTarget.TransformFromDevice;
                var workingAreaTopLeft = transform.Transform(new System.Windows.Point(workingArea.Left, workingArea.Top));
                var workingAreaSize = transform.Transform(new System.Windows.Point(workingArea.Width, workingArea.Height));
                
                Left = (workingAreaSize.X - Width) / 2 + workingAreaTopLeft.X;
                Top = (workingAreaSize.Y - Height) / 2 + workingAreaTopLeft.Y;
            }
            else
            {
                Left = (workingArea.Width - Width) / 2 + workingArea.Left;
                Top = (workingArea.Height - Height) / 2 + workingArea.Top;
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = _services.GetRequiredService<SettingsWindow>();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotKeyManager.DoubleAltPressed -= HotKeyManager_DoubleAltPressed;
            base.OnClosed(e);
        }
    }
}
