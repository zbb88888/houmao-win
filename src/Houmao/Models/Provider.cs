using System;
using System.Collections.Generic;

namespace Houmao.Models
{
    public class Provider
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public List<string> Models { get; set; } = new();
        public string ApiKey { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        
        // 用于 UI 绑定的模型文本（逗号分隔）
        public string ModelsText
        {
            get => string.Join(", ", Models);
            set => Models = string.IsNullOrEmpty(value) 
                ? new List<string>() 
                : new List<string>(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        
        // 用于列表显示的模型摘要
        public string ModelsSummary
        {
            get
            {
                if (Models.Count == 0) return "No models";
                if (Models.Count <= 2) return string.Join(", ", Models);
                return $"{Models[0]}, {Models[1]}, +{Models.Count - 2}";
            }
        }
        
        // 解析模型引用
        public ResolvedModel? ResolveModel(string? mention)
        {
            if (string.IsNullOrEmpty(mention))
            {
                // 使用默认模型
                return Models.Count > 0 
                    ? new ResolvedModel(this, Models[0]) 
                    : null;
            }
            
            // 按 Provider 名称匹配
            if (Name.Equals(mention, StringComparison.OrdinalIgnoreCase))
            {
                return Models.Count > 0 
                    ? new ResolvedModel(this, Models[0]) 
                    : null;
            }
            
            // 按模型 ID 匹配
            foreach (var model in Models)
            {
                if (model.Equals(mention, StringComparison.OrdinalIgnoreCase))
                {
                    return new ResolvedModel(this, model);
                }
            }
            
            return null;
        }
    }

    public class ResolvedModel
    {
        public Provider Provider { get; }
        public string ModelId { get; }

        public ResolvedModel(Provider provider, string modelId)
        {
            Provider = provider;
            ModelId = modelId;
        }
        
        public string Endpoint => $"{Provider.ApiUrl.TrimEnd('/')}/chat/completions";
        public string ApiKey => Provider.ApiKey;
    }
}