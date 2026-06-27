using System;

namespace Houmao.Models
{
    public class UsageRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ApplicationName { get; set; } = string.Empty;
        public string InputText { get; set; } = string.Empty;
        public string ResponseText { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsError { get; set; }
        public string? ErrorMessage { get; set; }
        
        // 用于历史面板显示的摘要
        public string Summary => InputText.Length > 100 
            ? InputText[..100] + "..." 
            : InputText;
            
        public string TimestampDisplay => Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }
}