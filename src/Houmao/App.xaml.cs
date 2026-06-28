using System;
using System.Drawing;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Houmao.Services;
using Houmao.ViewModels;
using Houmao.Views;
using Forms = System.Windows.Forms;

namespace Houmao
{
    public partial class App : Application
    {
        private ServiceProvider _services = null!;
        private Forms.NotifyIcon? _trayIcon;

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
            _trayIcon = new Forms.NotifyIcon
            {
                Text = "Houmao - AI Assistant",
                Icon = LoadTrayIcon(),
                Visible = true
            };
            
            var contextMenu = new Forms.ContextMenuStrip();
            
            var showItem = new Forms.ToolStripMenuItem("Show");
            showItem.Click += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var mainWindow = _services.GetRequiredService<MainWindow>();
                    mainWindow.ShowAndActivate();
                });
            };
            
            var providersItem = new Forms.ToolStripMenuItem("Providers");
            providersItem.Click += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var vm = _services.GetRequiredService<SettingsViewModel>();
                    vm.InitialPage = "providers";
                    var settingsWindow = new SettingsWindow(vm);
                    settingsWindow.ShowDialog();
                });
            };
            
            var generalItem = new Forms.ToolStripMenuItem("General");
            generalItem.Click += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var vm = _services.GetRequiredService<SettingsViewModel>();
                    vm.InitialPage = "general";
                    var settingsWindow = new SettingsWindow(vm);
                    settingsWindow.ShowDialog();
                });
            };
            
            var exitItem = new Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                Dispatcher.Invoke(Shutdown);
            };
            
            var settingsMenuItem = new Forms.ToolStripMenuItem("Settings");
            settingsMenuItem.DropDownItems.Add(providersItem);
            settingsMenuItem.DropDownItems.Add(generalItem);
            
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            contextMenu.Items.Add(exitItem);
            
            _trayIcon.ContextMenuStrip = contextMenu;
            
            _trayIcon.DoubleClick += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var mainWindow = _services.GetRequiredService<MainWindow>();
                    mainWindow.ShowAndActivate();
                });
            };
        }

        private static Icon LoadTrayIcon()
        {
            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Icons/houmao.ico"));
            if (resource?.Stream is null)
                throw new FileNotFoundException("Tray icon resource not found.");

            using var stream = resource.Stream;
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            memory.Position = 0;
            return new Icon(memory);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_trayIcon is not null)
            {
                _trayIcon.Visible = false;
            }
            _trayIcon?.Dispose();
            _services?.Dispose();
            base.OnExit(e);
        }
    }
}
