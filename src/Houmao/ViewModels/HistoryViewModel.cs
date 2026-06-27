using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Houmao.Models;
using Houmao.Services;
using Microsoft.Extensions.Logging;

namespace Houmao.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly ILogger<HistoryViewModel> _logger;
        private readonly IHistoryStore _historyStore;
        
        [ObservableProperty]
        private ObservableCollection<UsageRecord> _records = new();
        
        [ObservableProperty]
        private int _totalCount;
        
        [ObservableProperty]
        private bool _isLoading;
        
        [ObservableProperty]
        private bool _hasMore;
        
        private int _skip;
        private const int PageSize = 100;
        
        public HistoryViewModel(ILogger<HistoryViewModel> logger, IHistoryStore historyStore)
        {
            _logger = logger;
            _historyStore = historyStore;
            
            // 订阅历史变化事件
            _historyStore.HistoryChanged += async (s, e) => await RefreshAsync();
            
            // 初始加载
            _ = RefreshAsync();
        }
        
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (IsLoading) return;
            
            IsLoading = true;
            
            try
            {
                var records = await _historyStore.GetHistoryAsync(0, PageSize);
                var count = await _historyStore.GetCountAsync();
                
                Records.Clear();
                foreach (var record in records)
                {
                    Records.Add(record);
                }
                
                TotalCount = count;
                _skip = records.Count;
                HasMore = _skip < count;
                
                _logger.LogDebug("Refreshed history: {Count} records", records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh history");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsLoading || !HasMore) return;
            
            IsLoading = true;
            
            try
            {
                var records = await _historyStore.GetHistoryAsync(_skip, PageSize);
                
                foreach (var record in records)
                {
                    Records.Add(record);
                }
                
                _skip += records.Count;
                HasMore = _skip < TotalCount;
                
                _logger.LogDebug("Loaded more history: {Count} records", records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load more history");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task ClearAllAsync()
        {
            if (IsLoading) return;
            
            IsLoading = true;
            
            try
            {
                await _historyStore.ClearAsync();
                Records.Clear();
                TotalCount = 0;
                HasMore = false;
                
                _logger.LogInformation("Cleared all history");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear history");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}