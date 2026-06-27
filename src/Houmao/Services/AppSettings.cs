using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Houmao.Models;
using Microsoft.Extensions.Logging;

namespace Houmao.Services
{
    public class AppSettings : IAppSettings, IDisposable
    {
        private readonly ILogger<AppSettings> _logger;
        private readonly string _settingsPath;
        private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
        private CancellationTokenSource _debounceCts = new();
        
        private List<Provider> _providers = new();
        private bool _startWithWindows;
        private bool _selectToCopyEnabled;
        private bool _trackUsageHistory = true;
        private bool _followSystemTheme = true;
        private string _theme = "System";
        private double _windowLeft;
        private double _windowTop;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? SettingsChanged;
        
        public AppSettings(ILogger<AppSettings> logger) : this(logger, null)
        {
        }
        
        public AppSettings(ILogger<AppSettings> logger, string? settingsPath)
        {
            _logger = logger;
            _settingsPath = settingsPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "houmao",
                "settings.json");
        }
        
        public List<Provider> Providers
        {
            get => _providers;
            set
            {
                _providers = value;
                OnPropertyChanged();
                OnSettingsChanged();
            }
        }
        
        public Provider? DefaultProvider => _providers.Find(p => p.IsDefault);
        
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                _startWithWindows = value;
                OnPropertyChanged();
                OnSettingsChanged();
                UpdateStartupRegistry();
            }
        }
        
        public bool SelectToCopyEnabled
        {
            get => _selectToCopyEnabled;
            set
            {
                _selectToCopyEnabled = value;
                OnPropertyChanged();
                OnSettingsChanged();
            }
        }
        
        public bool TrackUsageHistory
        {
            get => _trackUsageHistory;
            set
            {
                _trackUsageHistory = value;
                OnPropertyChanged();
                OnSettingsChanged();
            }
        }
        
        public bool FollowSystemTheme
        {
            get => _followSystemTheme;
            set
            {
                _followSystemTheme = value;
                OnPropertyChanged();
                OnSettingsChanged();
            }
        }
        
        public string Theme
        {
            get => _theme;
            set
            {
                _theme = value;
                OnPropertyChanged();
                OnSettingsChanged();
            }
        }
        
        public double WindowLeft
        {
            get => _windowLeft;
            set
            {
                _windowLeft = value;
                OnPropertyChanged();
            }
        }
        
        public double WindowTop
        {
            get => _windowTop;
            set
            {
                _windowTop = value;
                OnPropertyChanged();
            }
        }
        
        public void Load()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    _logger.LogInformation("Settings file not found, using defaults");
                    
                    // 添加默认 Provider
                    if (_providers.Count == 0)
                    {
                        _providers.Add(new Provider
                        {
                            Name = "OpenAI",
                            ApiUrl = "https://api.openai.com/v1",
                            Models = new List<string> { "gpt-4", "gpt-3.5-turbo" },
                            IsDefault = true
                        });
                    }
                    
                    OnPropertyChanged(nameof(Providers));
                    OnPropertyChanged(nameof(DefaultProvider));
                    return;
                }
                
                var json = File.ReadAllText(_settingsPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = JsonSerializer.Deserialize<SettingsData>(json, options);
                
                if (data != null)
                {
                    _providers = data.Providers ?? new List<Provider>();
                    _startWithWindows = data.StartWithWindows;
                    _selectToCopyEnabled = data.SelectToCopyEnabled;
                    _trackUsageHistory = data.TrackUsageHistory;
                    _followSystemTheme = data.FollowSystemTheme;
                    _theme = data.Theme ?? "System";
                    _windowLeft = data.WindowLeft;
                    _windowTop = data.WindowTop;
                    
                    // 确保至少有一个 Provider
                    if (_providers.Count == 0)
                    {
                        _providers.Add(new Provider
                        {
                            Name = "OpenAI",
                            ApiUrl = "https://api.openai.com/v1",
                            Models = new List<string> { "gpt-4", "gpt-3.5-turbo" },
                            IsDefault = true
                        });
                    }
                    
                    // 确保有默认 Provider
                    if (!_providers.Exists(p => p.IsDefault) && _providers.Count > 0)
                    {
                        _providers[0].IsDefault = true;
                    }
                    
                    OnPropertyChanged(nameof(Providers));
                    OnPropertyChanged(nameof(DefaultProvider));
                    OnPropertyChanged(nameof(StartWithWindows));
                    OnPropertyChanged(nameof(SelectToCopyEnabled));
                    OnPropertyChanged(nameof(TrackUsageHistory));
                    OnPropertyChanged(nameof(FollowSystemTheme));
                    OnPropertyChanged(nameof(Theme));
                    OnPropertyChanged(nameof(WindowLeft));
                    OnPropertyChanged(nameof(WindowTop));
                    
                    _logger.LogInformation("Settings loaded from {Path}", _settingsPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
            }
        }
        
        public void Save()
        {
            SaveInternalAsync().ConfigureAwait(false);
        }
        
        public void SaveDebounced()
        {
            // 取消之前的保存操作
            _debounceCts.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            
            // 延迟 500ms 后保存
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, token);
                    if (!token.IsCancellationRequested)
                    {
                        await SaveInternalAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // 忽略取消异常
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in debounced save");
                }
            }, token);
        }
        
        private async Task SaveInternalAsync()
        {
            await _saveSemaphore.WaitAsync();
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var data = new SettingsData
                {
                    Providers = _providers,
                    StartWithWindows = _startWithWindows,
                    SelectToCopyEnabled = _selectToCopyEnabled,
                    TrackUsageHistory = _trackUsageHistory,
                    FollowSystemTheme = _followSystemTheme,
                    Theme = _theme,
                    WindowLeft = _windowLeft,
                    WindowTop = _windowTop
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(_settingsPath, json);
                
                _logger.LogInformation("Settings saved to {Path}", _settingsPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }
        
        public void AddProvider(Provider provider)
        {
            _providers.Add(provider);
            OnPropertyChanged(nameof(Providers));
            OnSettingsChanged();
        }
        
        public void UpdateProvider(Provider provider)
        {
            var index = _providers.FindIndex(p => p.Id == provider.Id);
            if (index >= 0)
            {
                _providers[index] = provider;
                OnPropertyChanged(nameof(Providers));
                OnPropertyChanged(nameof(DefaultProvider));
                OnSettingsChanged();
            }
        }
        
        public void DeleteProvider(Guid providerId)
        {
            var provider = _providers.Find(p => p.Id == providerId);
            if (provider != null)
            {
                _providers.Remove(provider);
                
                // 如果删除的是默认 Provider，设置第一个为默认
                if (provider.IsDefault && _providers.Count > 0)
                {
                    _providers[0].IsDefault = true;
                }
                
                OnPropertyChanged(nameof(Providers));
                OnPropertyChanged(nameof(DefaultProvider));
                OnSettingsChanged();
            }
        }
        
        public void SetDefaultProvider(Guid providerId)
        {
            foreach (var provider in _providers)
            {
                provider.IsDefault = provider.Id == providerId;
            }
            
            OnPropertyChanged(nameof(Providers));
            OnPropertyChanged(nameof(DefaultProvider));
            OnSettingsChanged();
        }
        
        public ResolvedModel? ResolveModel(string? mention)
        {
            if (string.IsNullOrEmpty(mention))
            {
                // 使用默认 Provider
                var defaultProvider = DefaultProvider;
                return defaultProvider?.ResolveModel(null);
            }
            
            // 按 Provider 名称匹配
            foreach (var provider in _providers)
            {
                if (provider.Name.Equals(mention, StringComparison.OrdinalIgnoreCase))
                {
                    return provider.ResolveModel(mention);
                }
            }
            
            // 按模型 ID 匹配
            foreach (var provider in _providers)
            {
                var resolved = provider.ResolveModel(mention);
                if (resolved != null)
                {
                    return resolved;
                }
            }
            
            return null;
        }
        
        private void UpdateStartupRegistry()
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key != null)
                {
                    if (_startWithWindows)
                    {
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        key.SetValue("Houmao", $"\"{exePath}\" --startup");
                    }
                    else
                    {
                        key.DeleteValue("Houmao", false);
                    }
                    
                    key.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update startup registry");
            }
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected virtual void OnSettingsChanged()
        {
            SaveDebounced();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        
        public void Dispose()
        {
            _debounceCts?.Dispose();
            _saveSemaphore?.Dispose();
        }
        
        private class SettingsData
        {
            public List<Provider> Providers { get; set; } = new();
            public bool StartWithWindows { get; set; }
            public bool SelectToCopyEnabled { get; set; }
            public bool TrackUsageHistory { get; set; } = true;
            public bool FollowSystemTheme { get; set; } = true;
            public string Theme { get; set; } = "System";
            public double WindowLeft { get; set; }
            public double WindowTop { get; set; }
        }
    }
}