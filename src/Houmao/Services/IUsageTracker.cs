using System;
using System.Threading.Tasks;
using Houmao.Models;

namespace Houmao.Services
{
    public interface IUsageTracker
    {
        /// <summary>
        /// 启动使用追踪
        /// </summary>
        void Start();
        
        /// <summary>
        /// 停止使用追踪
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 是否正在追踪
        /// </summary>
        bool IsTracking { get; }
        
        /// <summary>
        /// 获取当前前台应用信息
        /// </summary>
        Task<ApplicationInfo?> GetCurrentApplicationAsync();
        
        /// <summary>
        /// 获取当前焦点控件的文本
        /// </summary>
        Task<string?> GetFocusedTextAsync();
        
        /// <summary>
        /// 使用记录事件
        /// </summary>
        event EventHandler<UsageRecordEventArgs>? UsageRecorded;
    }
    
    public class ApplicationInfo
    {
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public int ProcessId { get; set; }
    }
    
    public class UsageRecordEventArgs : EventArgs
    {
        public UsageRecord Record { get; }
        
        public UsageRecordEventArgs(UsageRecord record)
        {
            Record = record;
        }
    }
}