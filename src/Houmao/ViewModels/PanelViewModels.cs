using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Houmao.Models;
using Houmao.Services;

namespace Houmao.ViewModels
{
    /// <summary>
    /// 帮助面板 ViewModel
    /// </summary>
    public partial class HelpPanelViewModel : ObservableObject
    {
        private readonly IAppSettings _settings;
        
        [ObservableProperty]
        private ObservableCollection<Provider> _providers = new();
        
        public HelpPanelViewModel(IAppSettings settings)
        {
            _settings = settings;
            LoadProviders();
            
            // 监听设置变化
            _settings.SettingsChanged += (s, e) => LoadProviders();
        }
        
        private void LoadProviders()
        {
            Providers.Clear();
            foreach (var provider in _settings.Providers)
            {
                Providers.Add(provider);
            }
        }
    }
    
    /// <summary>
    /// 历史面板 ViewModel
    /// </summary>
    public partial class HistoryPanelViewModel : ObservableObject
    {
        // 实际实现在 HistoryViewModel 中
    }
    
    /// <summary>
    /// 聊天面板 ViewModel
    /// </summary>
    public partial class ChatPanelViewModel : ObservableObject
    {
        // 聊天面板直接使用 MainViewModel 的消息列表
    }
}