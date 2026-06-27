using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Houmao.Models;

namespace Houmao.Services
{
    public interface IAiClient
    {
        /// <summary>
        /// 流式发送聊天请求
        /// </summary>
        IAsyncEnumerable<string> StreamAsync(
            ChatRequest request,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 非流式发送聊天请求
        /// </summary>
        Task<ChatMessage> AskAsync(
            ChatRequest request,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 发送问题并获取流式响应
        /// </summary>
        IAsyncEnumerable<string> AskStreamAsync(
            string question,
            List<ChatMessage> history,
            List<Attachment>? attachments,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 发送问题并获取完整响应
        /// </summary>
        Task<string> AskAsync(
            string question,
            List<ChatMessage> history,
            List<Attachment>? attachments,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 取消当前请求
        /// </summary>
        void CancelCurrentRequest();
        
        /// <summary>
        /// 当前是否有活动请求
        /// </summary>
        bool IsRequestActive { get; }
    }
}