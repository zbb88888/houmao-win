using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Houmao.Models;
using Houmao.Services;
using Microsoft.Extensions.Logging;

namespace Houmao.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly IAppSettings _settings;
        
        [ObservableProperty]
        private ObservableCollection<Provider> _providers = new();
        
        [ObservableProperty]
        private Provider? _selectedProvider;
        
        [ObservableProperty]
        private Provider? _editingProvider;
        
        [ObservableProperty]
        private bool _isEditing;
        
        [ObservableProperty]
        private bool _startWithWindows;
        
        [ObservableProperty]
        private bool _selectToCopyEnabled;
        
        [ObservableProperty]
        private bool _trackUsageHistory;
        
        [ObservableProperty]
        private bool _followSystemTheme;
        
        [ObservableProperty]
        private string _selectedTheme = "System";
        
        [ObservableProperty]
        private List<string> _themeOptions = new() { "Light", "Dark", "System" };
        
        public SettingsViewModel(ILogger<SettingsViewModel> logger, IAppSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }
        
        public void LoadSettings()
        {
            // 加载 Provider 列表
            Providers.Clear();
            foreach (var provider in _settings.Providers)
            {
                Providers.Add(provider);
            }
            
            // 加载设置
            StartWithWindows = _settings.StartWithWindows;
            SelectToCopyEnabled = _settings.SelectToCopyEnabled;
            TrackUsageHistory = _settings.TrackUsageHistory;
            FollowSystemTheme = _settings.FollowSystemTheme;
            SelectedTheme = _settings.Theme;
            
            // 选择第一个 Provider
            if (Providers.Count > 0)
            {
                SelectedProvider = Providers.FirstOrDefault(p => p.IsDefault) ?? Providers[0];
            }
        }
        
        [RelayCommand]
        private void AddProvider()
        {
            EditingProvider = new Provider
            {
                Name = "New Provider",
                ApiUrl = "https://api.openai.com/v1",
                Models = new List<string> { "gpt-4", "gpt-3.5-turbo" }
            };
            IsEditing = true;
        }
        
        [RelayCommand]
        private void EditProvider(Provider? provider)
        {
            if (provider == null) return;
            
            // 创建副本进行编辑
            EditingProvider = new Provider
            {
                Id = provider.Id,
                Name = provider.Name,
                ApiUrl = provider.ApiUrl,
                Models = new List<string>(provider.Models),
                ApiKey = provider.ApiKey,
                IsDefault = provider.IsDefault
            };
            IsEditing = true;
        }
        
        [RelayCommand]
        private void SaveProvider()
        {
            if (EditingProvider == null) return;
            
            // 检查是否是新增还是更新
            var existing = Providers.FirstOrDefault(p => p.Id == EditingProvider.Id);
            if (existing != null)
            {
                // 更新现有 Provider
                var index = Providers.IndexOf(existing);
                Providers[index] = EditingProvider;
                _settings.UpdateProvider(EditingProvider);
            }
            else
            {
                // 添加新 Provider
                Providers.Add(EditingProvider);
                _settings.AddProvider(EditingProvider);
            }
            
            EditingProvider = null;
            IsEditing = false;
            
            _logger.LogInformation("Provider saved");
        }
        
        [RelayCommand]
        private void CancelEdit()
        {
            EditingProvider = null;
            IsEditing = false;
        }
        
        [RelayCommand]
        private void DeleteProvider(Provider? provider)
        {
            if (provider == null) return;
            
            var result = MessageBox.Show(
                $"Are you sure you want to delete provider '{provider.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                Providers.Remove(provider);
                _settings.DeleteProvider(provider.Id);
                
                if (SelectedProvider == provider)
                {
                    SelectedProvider = Providers.FirstOrDefault();
                }
                
                _logger.LogInformation("Provider deleted: {Name}", provider.Name);
            }
        }
        
        [RelayCommand]
        private void SetDefaultProvider(Provider? provider)
        {
            if (provider == null) return;
            
            // 取消所有默认
            foreach (var p in Providers)
            {
                p.IsDefault = false;
            }
            
            // 设置新的默认
            provider.IsDefault = true;
            _settings.SetDefaultProvider(provider.Id);
            
            // 刷新列表
            var sorted = Providers.OrderByDescending(p => p.IsDefault).ToList();
            Providers.Clear();
            foreach (var p in sorted)
            {
                Providers.Add(p);
            }
            
            _logger.LogInformation("Default provider set to: {Name}", provider.Name);
        }
        
        [RelayCommand]
        private void SaveAll()
        {
            // 保存所有设置
            _settings.StartWithWindows = StartWithWindows;
            _settings.SelectToCopyEnabled = SelectToCopyEnabled;
            _settings.TrackUsageHistory = TrackUsageHistory;
            _settings.FollowSystemTheme = FollowSystemTheme;
            _settings.Theme = SelectedTheme;
            
            _settings.Save();
            
            _logger.LogInformation("All settings saved");
            
            MessageBox.Show("Settings saved successfully!", "Settings", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}