using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Houmao.Models;
using Microsoft.Extensions.Logging;

namespace Houmao.Services
{
    public class HistoryStore : IHistoryStore, IDisposable
    {
        private readonly ILogger<HistoryStore> _logger;
        private readonly string _historyPath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly List<UsageRecord> _records = new();
        private CancellationTokenSource _debounceCts = new();
        private bool _isDirty;
        
        public event EventHandler? HistoryChanged;
        
        public HistoryStore(ILogger<HistoryStore> logger)
        {
            _logger = logger;
            _historyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "houmao",
                "usage-history.json");
        }
        
        public async Task<List<UsageRecord>> GetHistoryAsync(int skip = 0, int take = 100)
        {
            await _semaphore.WaitAsync();
            try
            {
                // 确保数据已加载
                if (_records.Count == 0 && File.Exists(_historyPath))
                {
                    await LoadFromFileAsync();
                }
                
                // 返回倒序分页
                var count = Math.Min(take, _records.Count - skip);
                if (count <= 0) return new List<UsageRecord>();
                
                var result = new List<UsageRecord>();
                for (int i = _records.Count - 1 - skip; i >= _records.Count - skip - count; i--)
                {
                    if (i >= 0 && i < _records.Count)
                    {
                        result.Add(_records[i]);
                    }
                }
                
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public async Task AppendAsync(UsageRecord record)
        {
            await _semaphore.WaitAsync();
            try
            {
                _records.Add(record);
                _isDirty = true;
                
                // 防抖保存
                DebounceSave();
                
                // 触发变化事件
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public async Task ClearAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _records.Clear();
                _isDirty = true;
                
                // 立即保存
                await SaveToFileAsync();
                
                // 触发变化事件
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public async Task<int> GetCountAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _records.Count;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        private async Task LoadFromFileAsync()
        {
            try
            {
                if (!File.Exists(_historyPath))
                {
                    _logger.LogInformation("History file not found, starting fresh");
                    return;
                }
                
                var json = await File.ReadAllTextAsync(_historyPath);
                var records = JsonSerializer.Deserialize<List<UsageRecord>>(json);
                
                if (records != null)
                {
                    _records.Clear();
                    _records.AddRange(records);
                    _logger.LogInformation("Loaded {Count} history records", _records.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load history from {Path}", _historyPath);
            }
        }
        
        private async Task SaveToFileAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_historyPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(_records, options);
                await File.WriteAllTextAsync(_historyPath, json);
                
                _isDirty = false;
                _logger.LogInformation("Saved {Count} history records", _records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save history to {Path}", _historyPath);
            }
        }
        
        private void DebounceSave()
        {
            // 取消之前的保存操作
            _debounceCts.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            
            // 延迟 2 秒后保存
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000, token);
                    if (!token.IsCancellationRequested && _isDirty)
                    {
                        await _semaphore.WaitAsync(token);
                        try
                        {
                            await SaveToFileAsync();
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
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
        
        public void Dispose()
        {
            _debounceCts?.Dispose();
            _semaphore?.Dispose();
        }
    }
}