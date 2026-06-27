using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Houmao.Models;

namespace Houmao.Services
{
    public interface IHistoryStore
    {
        /// <summary>
        /// 获取历史记录
        /// </summary>
        Task<List<UsageRecord>> GetHistoryAsync(int skip = 0, int take = 100);
        
        /// <summary>
        /// 添加历史记录
        /// </summary>
        Task AppendAsync(UsageRecord record);
        
        /// <summary>
        /// 清空历史记录
        /// </summary>
        Task ClearAsync();
        
        /// <summary>
        /// 获取历史记录总数
        /// </summary>
        Task<int> GetCountAsync();
        
        /// <summary>
        /// 历史记录变化事件
        /// </summary>
        event EventHandler? HistoryChanged;
    }
}