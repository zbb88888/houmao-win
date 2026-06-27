using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Houmao.Models;
using Houmao.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Houmao.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IAiClient _aiClient;
        private readonly IAppSettings _settings;
        private readonly IHistoryStore _historyStore;
        private readonly IServiceProvider _serviceProvider;
        
        private readonly List<ChatMessage> _conversationHistory = new();
        private readonly CommandHistory _commandHistory = new();
        private CancellationTokenSource? _currentRequestCts;
        
        [ObservableProperty]
        private string _inputText = string.Empty;
        
        [ObservableProperty]
        private string _statusText = "Ready";
        
        [ObservableProperty]
        private string _currentProviderName = string.Empty;
        
        [ObservableProperty]
        private int _messageCount;
        
        [ObservableProperty]
        private bool _isLoading;
        
        [ObservableProperty]
        private object? _currentPanel;
        
        [ObservableProperty]
        private ObservableCollection<Attachment> _attachments = new();
        
        [ObservableProperty]
        private ObservableCollection<ChatMessageViewModel> _messages = new();
        
        public MainViewModel(
            ILogger<MainViewModel> logger,
            IAiClient aiClient,
            IAppSettings settings,
            IHistoryStore historyStore,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _aiClient = aiClient;
            _settings = settings;
            _historyStore = historyStore;
            _serviceProvider = serviceProvider;
            
            // 更新 Provider 名称
            UpdateProviderName();
            _settings.SettingsChanged += (s, e) => UpdateProviderName();
        }
        
        private void UpdateProviderName()
        {
            CurrentProviderName = _settings.DefaultProvider?.Name ?? "No Provider";
        }
        
        [RelayCommand]
        private async Task SubmitAsync()
        {
            var input = InputText?.Trim();
            if (string.IsNullOrEmpty(input)) return;
            
            // 处理特殊命令
            if (input.Length == 1)
            {
                switch (input.ToLower())
                {
                    case "b":
                        TogglePanel<HistoryPanelViewModel>();
                        ClearInput();
                        return;
                    case "h":
                        TogglePanel<HelpPanelViewModel>();
                        ClearInput();
                        return;
                }
            }
            
            // 添加到命令历史
            _commandHistory.Add(input);
            
            // 解析 @model 路由
            var (mention, message) = ParseMention(input);
            var resolvedModel = _settings.ResolveModel(mention);
            
            if (resolvedModel == null)
            {
                StatusText = "No provider configured";
                return;
            }
            
            // 创建用户消息
            var userMessage = ChatMessage.CreateUserMessage(message, new List<Attachment>(Attachments));
            _conversationHistory.Add(userMessage);
            
            // 添加用户消息到显示列表
            Messages.Add(new ChatMessageViewModel(userMessage, true));
            
            // 清空输入和附件
            ClearInput();
            Attachments.Clear();
            
            // 开始请求
            IsLoading = true;
            StatusText = "Thinking...";
            _currentRequestCts?.Cancel();
            _currentRequestCts = new CancellationTokenSource();
            
            try
            {
                // 流式请求
                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var token in _aiClient.AskStreamAsync(
                    message,
                    _conversationHistory,
                    null, // 附件已在 userMessage 中
                    _currentRequestCts.Token))
                {
                    responseBuilder.Append(token);
                    StatusText = $"Response: {responseBuilder.Length} characters";
                }
                
                var response = responseBuilder.ToString();
                
                // 添加助手消息到历史
                var assistantMessage = ChatMessage.CreateAssistantMessage(response);
                _conversationHistory.Add(assistantMessage);
                
                // 添加助手消息到显示列表
                Messages.Add(new ChatMessageViewModel(assistantMessage, false));
                
                // 限制历史长度
                if (_conversationHistory.Count > 20)
                {
                    _conversationHistory.RemoveRange(0, _conversationHistory.Count - 20);
                }
                
                MessageCount = _conversationHistory.Count;
                StatusText = "Ready";
                
                // 记录使用情况
                await RecordUsage(input, response, resolvedModel);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Request cancelled";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI request");
                StatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private void CancelRequest()
        {
            _currentRequestCts?.Cancel();
            _aiClient.CancelCurrentRequest();
            StatusText = "Request cancelled";
            IsLoading = false;
        }
        
        [RelayCommand]
        public void ClearConversation()
        {
            _conversationHistory.Clear();
            Messages.Clear();
            MessageCount = 0;
            StatusText = "Conversation cleared";
        }
        
        [RelayCommand]
        private void AttachFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp|" +
                         "Audio files (*.mp3;*.wav;*.ogg;*.flac;*.m4a;*.aac)|*.mp3;*.wav;*.ogg;*.flac;*.m4a;*.aac|" +
                         "All files (*.*)|*.*"
            };
            
            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    try
                    {
                        var attachment = Attachment.FromFile(file);
                        Attachments.Add(attachment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to add attachment: {File}", file);
                    }
                }
            }
        }
        
        [RelayCommand]
        private void RemoveAttachment(Attachment attachment)
        {
            Attachments.Remove(attachment);
        }
        
        public string? GetPreviousCommand()
        {
            return _commandHistory.Previous();
        }
        
        public string? GetNextCommand()
        {
            return _commandHistory.Next();
        }
        
        private void ClearInput()
        {
            InputText = string.Empty;
            _commandHistory.Reset();
        }
        
        private (string? mention, string message) ParseMention(string input)
        {
            if (!input.StartsWith("@"))
                return (null, input);
            
            var spaceIndex = input.IndexOf(' ');
            if (spaceIndex < 0)
                return (input[1..], string.Empty);
            
            var mention = input[1..spaceIndex];
            var message = input[(spaceIndex + 1)..];
            return (mention, message);
        }
        
        private void TogglePanel<T>() where T : class
        {
            if (CurrentPanel is T)
            {
                CurrentPanel = null;
            }
            else
            {
                // 使用 DI 容器创建面板 ViewModel
                CurrentPanel = _serviceProvider.GetRequiredService<T>();
            }
        }
        
        private async Task RecordUsage(string input, string response, ResolvedModel model)
        {
            try
            {
                var record = new UsageRecord
                {
                    ApplicationName = "Houmao",
                    InputText = input,
                    ResponseText = response,
                    ProviderName = model.Provider.Name,
                    ModelId = model.ModelId,
                    Timestamp = DateTime.UtcNow
                };
                
                await _historyStore.AppendAsync(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record usage");
            }
        }
    }
    
    public class CommandHistory
    {
        private readonly List<string> _history = new();
        private int _currentIndex = -1;
        private string? _currentInput;
        
        public void Add(string command)
        {
            // 去重
            _history.Remove(command);
            _history.Add(command);
            
            // 限制历史长度
            if (_history.Count > 100)
            {
                _history.RemoveAt(0);
            }
            
            Reset();
        }
        
        public string? Previous()
        {
            if (_history.Count == 0) return null;
            
            if (_currentIndex < 0)
            {
                _currentIndex = _history.Count - 1;
            }
            else if (_currentIndex > 0)
            {
                _currentIndex--;
            }
            
            return _history[_currentIndex];
        }
        
        public string? Next()
        {
            if (_history.Count == 0 || _currentIndex < 0) return null;
            
            if (_currentIndex < _history.Count - 1)
            {
                _currentIndex++;
                return _history[_currentIndex];
            }
            else
            {
                _currentIndex = -1;
                return _currentInput;
            }
        }
        
        public void Reset()
        {
            _currentIndex = -1;
            _currentInput = null;
        }
    }
    
}