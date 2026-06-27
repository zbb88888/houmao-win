using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Houmao.Services;
using Houmao.ViewModels;
using Houmao.Views;
using H.NotifyIcon;

namespace Houmao
{
    public partial class App : Application
    {
        private ServiceProvider _services = null!;
        private TaskbarIcon? _trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var services = new ServiceCollection();
            ConfigureServices(services);
            _services = services.BuildServiceProvider();

            var mainWindow = _services.GetRequiredService<MainWindow>();
            
            _services.GetRequiredService<HotKeyManager>();
            
            // 先加载设置
            var settings = _services.GetRequiredService<IAppSettings>();
            settings.Load();
            
            // 再启动需要设置的服务
            _services.GetRequiredService<SelectToCopyManager>();
            _services.GetRequiredService<IUsageTracker>().Start();

            CreateTrayIcon();
            
            mainWindow.Hide();

            if (!e.Args.Contains("--startup"))
                mainWindow.ShowAndActivate();
        }

        private static void ConfigureServices(IServiceCollection s)
        {
            s.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            s.AddSingleton<IAppSettings, AppSettings>();
            s.AddSingleton<IHistoryStore, HistoryStore>();
            s.AddSingleton<IUsageTracker, UsageTracker>();
            s.AddSingleton<HotKeyManager>();
            s.AddSingleton<SelectToCopyManager>();
            s.AddHttpClient<IAiClient, AiClient>();

            s.AddSingleton<MainViewModel>();
            s.AddSingleton<HistoryViewModel>();
            s.AddTransient<SettingsViewModel>();
            s.AddTransient<HelpPanelViewModel>();
            s.AddTransient<ChatPanelViewModel>();

            s.AddSingleton<MainWindow>();
            s.AddTransient<SettingsWindow>();
        }

        private void CreateTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "Houmao - AI Assistant",
                IconSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/houmao.ico"))
            };
            
            var contextMenu = new ContextMenu();
            
            var showItem = new MenuItem { Header = "Show" };
            showItem.Click += (s, e) =>
            {
                var mainWindow = _services.GetRequiredService<MainWindow>();
                mainWindow.ShowAndActivate();
            };
            
            var settingsItem = new MenuItem { Header = "Settings" };
            settingsItem.Click += (s, e) =>
            {
                var settingsWindow = _services.GetRequiredService<SettingsWindow>();
                settingsWindow.ShowDialog();
            };
            
            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) =>
            {
                Shutdown();
            };
            
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitItem);
            
            _trayIcon.ContextMenu = contextMenu;
            
            _trayIcon.DoubleClickCommand = new RelayCommand(() =>
            {
                var mainWindow = _services.GetRequiredService<MainWindow>();
                mainWindow.ShowAndActivate();
            });
            
            _trayIcon.ForceCreate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _services?.Dispose();
            base.OnExit(e);
        }
    }
}
