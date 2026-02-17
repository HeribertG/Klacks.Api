using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class LLMCapabilityService
{
    private static readonly Dictionary<string, LLMProviderType> ProviderMapping = new()
    {
        { "openai", LLMProviderType.OpenAI },
        { "anthropic", LLMProviderType.Anthropic },
        { "google", LLMProviderType.Google },
        { "azure", LLMProviderType.Azure },
        { "local", LLMProviderType.Local },
        { "baidu", LLMProviderType.Baidu },
        { "alibaba", LLMProviderType.Alibaba },
        { "tencent", LLMProviderType.Tencent },
        { "bytedance", LLMProviderType.ByteDance },
        { "mistral", LLMProviderType.Mistral },
        { "cohere", LLMProviderType.Cohere },
        { "huggingface", LLMProviderType.HuggingFace }
    };

    public static LLMCapability[] GetCapabilities(LLMModel model)
    {
        var capabilities = new List<LLMCapability> { LLMCapability.Chat };
        
        if (!ProviderMapping.TryGetValue(model.ProviderId.ToLower(), out var providerType))
        {
            return capabilities.ToArray();
        }

        switch (providerType)
        {
            case LLMProviderType.OpenAI:
                AddOpenAICapabilities(capabilities, model);
                break;
            case LLMProviderType.Anthropic:
                AddAnthropicCapabilities(capabilities, model);
                break;
            case LLMProviderType.Google:
                AddGoogleCapabilities(capabilities, model);
                break;
            case LLMProviderType.Azure:
                AddAzureCapabilities(capabilities, model);
                break;
            case LLMProviderType.Baidu:
                AddBaiduCapabilities(capabilities, model);
                break;
            case LLMProviderType.Alibaba:
                AddAlibabaCapabilities(capabilities, model);
                break;
            case LLMProviderType.Tencent:
                AddTencentCapabilities(capabilities, model);
                break;
            case LLMProviderType.ByteDance:
                AddByteDanceCapabilities(capabilities, model);
                break;
            case LLMProviderType.Mistral:
                AddMistralCapabilities(capabilities, model);
                break;
            case LLMProviderType.Cohere:
                AddCohereCapabilities(capabilities, model);
                break;
            case LLMProviderType.HuggingFace:
                AddHuggingFaceCapabilities(capabilities, model);
                break;
            case LLMProviderType.Local:
                AddLocalCapabilities(capabilities, model);
                break;
        }

        return capabilities.ToArray();
    }

    private static void AddOpenAICapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        capabilities.Add(LLMCapability.FunctionCalling);
        
        if (model.ModelId.Contains("gpt-4"))
        {
            capabilities.Add(LLMCapability.Vision);
            capabilities.Add(LLMCapability.CodeGeneration);
        }
        
        if (model.ModelId.Contains("dall-e"))
        {
            capabilities.Add(LLMCapability.ImageGeneration);
        }
    }

    private static void AddAnthropicCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        if (!model.ModelId.Contains("claude-instant"))
        {
            capabilities.Add(LLMCapability.FunctionCalling);
        }
        
        if (model.ModelId.Contains("claude-3"))
        {
            capabilities.Add(LLMCapability.Vision);
            capabilities.Add(LLMCapability.CodeGeneration);
        }
    }

    private static void AddGoogleCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        if (model.ModelId.Contains("gemini"))
        {
            capabilities.Add(LLMCapability.FunctionCalling);
            capabilities.Add(LLMCapability.Vision);
            capabilities.Add(LLMCapability.CodeGeneration);
        }
    }

    private static void AddAzureCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        capabilities.Add(LLMCapability.FunctionCalling);
        
        if (model.ModelId.Contains("gpt-4"))
        {
            capabilities.Add(LLMCapability.Vision);
        }
    }

    private static void AddBaiduCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        if (model.ModelId.Contains("ernie"))
        {
            capabilities.Add(LLMCapability.FunctionCalling);
            capabilities.Add(LLMCapability.CodeGeneration);
        }
    }

    private static void AddAlibabaCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        if (model.ModelId.Contains("qwen"))
        {
            capabilities.Add(LLMCapability.FunctionCalling);
            capabilities.Add(LLMCapability.CodeGeneration);
        }
    }

    private static void AddTencentCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        if (model.ModelId.Contains("hunyuan"))
        {
            capabilities.Add(LLMCapability.FunctionCalling);
        }
    }

    private static void AddByteDanceCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        if (model.ModelId.Contains("doubao"))
        {
            capabilities.Add(LLMCapability.FunctionCalling);
        }
    }

    private static void AddMistralCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        capabilities.Add(LLMCapability.FunctionCalling);
        capabilities.Add(LLMCapability.CodeGeneration);
    }

    private static void AddCohereCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        capabilities.Add(LLMCapability.FunctionCalling);
        capabilities.Add(LLMCapability.Embedding);
    }

    private static void AddHuggingFaceCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        capabilities.Add(LLMCapability.CodeGeneration);
        
        if (model.ModelId.Contains("llava"))
        {
            capabilities.Add(LLMCapability.Vision);
        }
    }

    private static void AddLocalCapabilities(List<LLMCapability> capabilities, LLMModel model)
    {
        capabilities.Add(LLMCapability.CodeGeneration);
    }
}