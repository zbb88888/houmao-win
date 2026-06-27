using System;
using System.Collections.Generic;
using System.ComponentModel;
using Houmao.Models;

namespace Houmao.Services
{
    public interface IAppSettings : INotifyPropertyChanged
    {
        // Provider 管理
        List<Provider> Providers { get; set; }
        Provider? DefaultProvider { get; }
        
        // 通用设置
        bool StartWithWindows { get; set; }
        bool SelectToCopyEnabled { get; set; }
        bool TrackUsageHistory { get; set; }
        bool FollowSystemTheme { get; set; }
        string Theme { get; set; } // "Light", "Dark", "System"
        
        // 窗口位置
        double WindowLeft { get; set; }
        double WindowTop { get; set; }
        
        // 方法
        void Load();
        void Save();
        void SaveDebounced();
        
        // Provider 操作
        void AddProvider(Provider provider);
        void UpdateProvider(Provider provider);
        void DeleteProvider(Guid providerId);
        void SetDefaultProvider(Guid providerId);
        
        // 模型解析
        ResolvedModel? ResolveModel(string? mention);
        
        // 事件
        event EventHandler? SettingsChanged;
    }
}