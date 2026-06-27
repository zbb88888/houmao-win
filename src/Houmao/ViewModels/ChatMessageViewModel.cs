using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Houmao.Models;

namespace Houmao.ViewModels
{
    public partial class ChatMessageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _content = string.Empty;
        
        [ObservableProperty]
        private bool _isUser;
        
        [ObservableProperty]
        private DateTime _timestamp;
        
        [ObservableProperty]
        private string _providerName = string.Empty;
        
        [ObservableProperty]
        private string _modelId = string.Empty;
        
        // 用于显示的模型名称
        public string? ModelName => IsUser ? null : (string.IsNullOrEmpty(ModelId) ? null : ModelId);
        
        public ChatMessageViewModel()
        {
            Timestamp = DateTime.UtcNow;
        }
        
        public ChatMessageViewModel(ChatMessage message, bool isUser = false)
        {
            Content = message.Content?.ToString() ?? string.Empty;
            IsUser = isUser;
            Timestamp = DateTime.UtcNow;
        }
        
        public ChatMessageViewModel(string content, bool isUser = false)
        {
            Content = content;
            IsUser = isUser;
            Timestamp = DateTime.UtcNow;
        }
    }
}