using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Houmao.Models
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";
        
        [JsonPropertyName("content")]
        public object? Content { get; set; }
        
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
        
        // 用于多模态内容
        public static ChatMessage CreateUserMessage(string text, List<Attachment>? attachments = null)
        {
            if (attachments == null || attachments.Count == 0)
            {
                return new ChatMessage
                {
                    Role = "user",
                    Content = text
                };
            }
            
            // 创建多模态内容
            var contentParts = new List<object>();
            
            // 添加文本部分
            contentParts.Add(new { type = "text", text });
            
            // 添加附件
            foreach (var attachment in attachments)
            {
                if (attachment.Type == AttachmentType.Image)
                {
                    contentParts.Add(new
                    {
                        type = "image_url",
                        image_url = new { url = attachment.ToDataUri() }
                    });
                }
                else if (attachment.Type == AttachmentType.Audio)
                {
                    contentParts.Add(new
                    {
                        type = "input_audio",
                        input_audio = new
                        {
                            data = attachment.Base64Data,
                            format = attachment.MimeType.Split('/')[1]
                        }
                    });
                }
            }
            
            return new ChatMessage
            {
                Role = "user",
                Content = contentParts
            };
        }
        
        public static ChatMessage CreateAssistantMessage(string content)
        {
            return new ChatMessage
            {
                Role = "assistant",
                Content = content
            };
        }
        
        public static ChatMessage CreateSystemMessage(string content)
        {
            return new ChatMessage
            {
                Role = "system",
                Content = content
            };
        }
    }

    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();
        
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;
        
        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }
        
        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }
    }

    public class ChatStreamChunk
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("object")]
        public string? Object { get; set; }
        
        [JsonPropertyName("created")]
        public long Created { get; set; }
        
        [JsonPropertyName("model")]
        public string? Model { get; set; }
        
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
        
        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
        
        [JsonPropertyName("delta")]
        public Delta? Delta { get; set; }
        
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
        
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class Delta
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        
        [JsonPropertyName("reasoning_content")]
        public string? ReasoningContent { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}