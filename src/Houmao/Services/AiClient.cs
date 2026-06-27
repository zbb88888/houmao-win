using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Houmao.Models;
using Microsoft.Extensions.Logging;

namespace Houmao.Services
{
    public class AiClient : IAiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiClient> _logger;
        private readonly IAppSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;
        private CancellationTokenSource? _currentRequestCts;
        private volatile bool _isRequestActive;
        
        public bool IsRequestActive => _isRequestActive;
        
        public AiClient(HttpClient httpClient, ILogger<AiClient> logger, IAppSettings settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }
        
        public async IAsyncEnumerable<string> StreamAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _isRequestActive = true;
            _currentRequestCts?.Dispose();
            _currentRequestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _currentRequestCts.Token;
            
            try
            {
                // 解析模型引用
                var resolvedModel = _settings.ResolveModel(null);
                if (resolvedModel == null)
                {
                    throw new InvalidOperationException("No default provider configured");
                }
                
                request.Model = resolvedModel.ModelId;
                
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, resolvedModel.Endpoint);
                httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);
                httpRequest.Headers.Add("Authorization", $"Bearer {resolvedModel.ApiKey}");
                
                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);
                
                response.EnsureSuccessStatusCode();
                
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);
                
                // 推理标签过滤状态机
                var insideThink = false;
                var thinkBuffer = string.Empty;
                
                while (!reader.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line == null) break;
                    
                    // 跳过空行
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // 解析 SSE 数据
                    if (!line.StartsWith("data: ")) continue;
                    
                    var data = line["data: ".Length..];
                    
                    // 检查结束标记
                    if (data == "[DONE]") break;
                    
                    ChatStreamChunk? chunk;
                    try
                    {
                        chunk = JsonSerializer.Deserialize<ChatStreamChunk>(data, _jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse SSE chunk: {Data}", data);
                        continue;
                    }
                    
                    var token = chunk?.Choices?[0]?.Delta?.Content;
                    var reasoningToken = chunk?.Choices?[0]?.Delta?.ReasoningContent;
                    
                    // 优先使用 content，如果为空则使用 reasoning_content
                    var contentToProcess = !string.IsNullOrEmpty(token) ? token : reasoningToken;
                    
                    if (string.IsNullOrEmpty(contentToProcess)) continue;
                    
                    // 推理标签过滤状态机
                    var filteredToken = ProcessThinkTags(contentToProcess, ref insideThink, ref thinkBuffer);
                    if (!string.IsNullOrEmpty(filteredToken))
                    {
                        yield return filteredToken;
                    }
                }
                
                // 处理缓冲区中剩余的内容
                if (!insideThink && !string.IsNullOrEmpty(thinkBuffer))
                {
                    yield return thinkBuffer;
                }
            }
            finally
            {
                _isRequestActive = false;
                _currentRequestCts?.Dispose();
                _currentRequestCts = null;
            }
        }
        
        public async Task<ChatMessage> AskAsync(
            ChatRequest request,
            CancellationToken cancellationToken = default)
        {
            _isRequestActive = true;
            _currentRequestCts?.Dispose();
            _currentRequestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _currentRequestCts.Token;
            
            try
            {
                // 解析模型引用
                var resolvedModel = _settings.ResolveModel(null);
                if (resolvedModel == null)
                {
                    throw new InvalidOperationException("No default provider configured");
                }
                
                request.Model = resolvedModel.ModelId;
                request.Stream = false;
                
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, resolvedModel.Endpoint);
                httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);
                httpRequest.Headers.Add("Authorization", $"Bearer {resolvedModel.ApiKey}");
                
                using var response = await _httpClient.SendAsync(httpRequest, ct);
                response.EnsureSuccessStatusCode();
                
                var chunk = await response.Content.ReadFromJsonAsync<ChatStreamChunk>(_jsonOptions, ct);
                var content = chunk?.Choices?[0]?.Message?.Content?.ToString() ?? string.Empty;
                
                // 非流式过滤推理标签
                content = FilterThinkTags(content);
                
                return ChatMessage.CreateAssistantMessage(content);
            }
            finally
            {
                _isRequestActive = false;
                _currentRequestCts?.Dispose();
                _currentRequestCts = null;
            }
        }
        
        public async IAsyncEnumerable<string> AskStreamAsync(
            string question,
            List<ChatMessage> history,
            List<Attachment>? attachments,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 构建消息列表
            var messages = new List<ChatMessage>(history);
            
            // 添加当前问题
            var userMessage = ChatMessage.CreateUserMessage(question, attachments);
            messages.Add(userMessage);
            
            // 创建请求
            var request = new ChatRequest
            {
                Messages = messages,
                Stream = true,
                Temperature = 0.7,
                MaxTokens = 2000
            };
            
            // 流式发送
            await foreach (var token in StreamAsync(request, cancellationToken))
            {
                yield return token;
            }
        }
        
        public async Task<string> AskAsync(
            string question,
            List<ChatMessage> history,
            List<Attachment>? attachments,
            CancellationToken cancellationToken = default)
        {
            // 构建消息列表
            var messages = new List<ChatMessage>(history);
            
            // 添加当前问题
            var userMessage = ChatMessage.CreateUserMessage(question, attachments);
            messages.Add(userMessage);
            
            // 创建请求
            var request = new ChatRequest
            {
                Messages = messages,
                Stream = false,
                Temperature = 0.7,
                MaxTokens = 2000
            };
            
            // 非流式发送
            var response = await AskAsync(request, cancellationToken);
            return response.Content?.ToString() ?? string.Empty;
        }
        
        public void CancelCurrentRequest()
        {
            _currentRequestCts?.Cancel();
        }
        
        /// <summary>
        /// 处理流式推理标签过滤
        /// </summary>
        private string ProcessThinkTags(string token, ref bool insideThink, ref string thinkBuffer)
        {
            var result = new System.Text.StringBuilder();
            
            // 将 token 添加到缓冲区
            thinkBuffer += token;
            
            while (thinkBuffer.Length > 0)
            {
                if (!insideThink)
                {
                    // 检查是否进入 think 标签
                    int thinkStart = thinkBuffer.IndexOf("<think>");
                    if (thinkStart >= 0)
                    {
                        // 输出 think 标签之前的内容
                        if (thinkStart > 0)
                        {
                            result.Append(thinkBuffer.Substring(0, thinkStart));
                        }
                        
                        // 移除已处理的内容
                        thinkBuffer = thinkBuffer.Substring(thinkStart + 7); // 7 = "<think>".Length
                        insideThink = true;
                    }
                    else
                    {
                        // 检查是否可能是部分 think 标签
                        // think 标签是 "<think>"，检查缓冲区末尾是否匹配部分前缀
                        string partialTag = GetPartialTagMatch(thinkBuffer, "<think>");
                        if (partialTag.Length > 0 && thinkBuffer.Length > partialTag.Length)
                        {
                            // 输出部分标签之前的内容
                            result.Append(thinkBuffer.Substring(0, thinkBuffer.Length - partialTag.Length));
                            thinkBuffer = partialTag;
                        }
                        else if (partialTag.Length == 0)
                        {
                            // 没有可能的部分标签，输出所有内容
                            result.Append(thinkBuffer);
                            thinkBuffer = string.Empty;
                        }
                        break;
                    }
                }
                else
                {
                    // 检查是否离开 think 标签
                    int thinkEnd = thinkBuffer.IndexOf("</think>");
                    if (thinkEnd >= 0)
                    {
                        // 移除已处理的内容
                        thinkBuffer = thinkBuffer.Substring(thinkEnd + 8); // 8 = "</think>".Length
                        insideThink = false;
                    }
                    else
                    {
                        // 没有找到结束标签，检查是否可能是部分结束标签
                        string partialTag = GetPartialTagMatch(thinkBuffer, "</think>");
                        if (partialTag.Length > 0)
                        {
                            thinkBuffer = partialTag;
                        }
                        else
                        {
                            thinkBuffer = string.Empty;
                        }
                        break;
                    }
                }
            }
            
            return result.ToString();
        }
        
        /// <summary>
        /// 检查缓冲区末尾是否匹配标签的前缀
        /// </summary>
        private string GetPartialTagMatch(string buffer, string tag)
        {
            // 从最长的可能前缀开始检查
            for (int len = Math.Min(tag.Length - 1, buffer.Length); len > 0; len--)
            {
                if (buffer.EndsWith(tag.Substring(0, len)))
                {
                    return tag.Substring(0, len);
                }
            }
            return string.Empty;
        }
        
        /// <summary>
        /// 非流式过滤推理标签（正则）
        /// </summary>
        private string FilterThinkTags(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            
            // 使用正则表达式过滤所有 <think>...</think> 标签
            var pattern = @"<think>[\s\S]*?</think>";
            var filtered = Regex.Replace(content, pattern, string.Empty);
            
            // 去除首尾空白
            filtered = filtered.Trim();
            
            // 如果过滤后为空，返回原始内容
            return string.IsNullOrEmpty(filtered) ? content : filtered;
        }
    }
}
