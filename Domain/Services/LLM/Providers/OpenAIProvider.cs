using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers;

public class OpenAIProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly IConfiguration _configuration;
    private string _apiKey = string.Empty;
    private Models.LLM.LLMProvider? _providerConfig;

    public string ProviderId => "openai";
    public string ProviderName => "OpenAI";
    public bool IsEnabled => _providerConfig?.IsEnabled ?? false;

    public OpenAIProvider(HttpClient httpClient, ILogger<OpenAIProvider> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
    }

    public void Configure(Models.LLM.LLMProvider providerConfig)
    {
        _providerConfig = providerConfig;
        _apiKey = providerConfig.ApiKey ?? string.Empty;
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
        
        if (!string.IsNullOrEmpty(providerConfig.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(providerConfig.BaseUrl);
        }
    }

    public async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request)
    {
        if (!IsEnabled)
        {
            return new LLMProviderResponse 
            { 
                Success = false, 
                Error = "OpenAI provider is not enabled" 
            };
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return new LLMProviderResponse 
            { 
                Success = false, 
                Error = "Der Provider für das gewählte Modell ist nicht verfügbar." 
            };
        }

        try
        {
            var openAIRequest = new OpenAIRequest
            {
                Model = MapModelId(request.ModelId),
                Messages = BuildMessages(request),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Functions = MapFunctions(request.AvailableFunctions),
                FunctionCall = request.AvailableFunctions.Any() ? "auto" : null
            };

            var json = JsonSerializer.Serialize(openAIRequest, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, error);
                return new LLMProviderResponse 
                { 
                    Success = false, 
                    Error = $"OpenAI API error: {response.StatusCode}" 
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (openAIResponse?.Choices == null || !openAIResponse.Choices.Any())
            {
                return new LLMProviderResponse 
                { 
                    Success = false, 
                    Error = "Invalid response from OpenAI" 
                };
            }

            var choice = openAIResponse.Choices[0];
            var result = new LLMProviderResponse
            {
                Content = choice.Message?.Content ?? string.Empty,
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = openAIResponse.Usage?.PromptTokens ?? 0,
                    OutputTokens = openAIResponse.Usage?.CompletionTokens ?? 0,
                    Cost = CalculateCost(request.ModelId, openAIResponse.Usage)
                }
            };

            // Handle function calls if any
            if (choice.Message?.FunctionCall != null)
            {
                result.FunctionCalls.Add(new LLMFunctionCall
                {
                    FunctionName = choice.Message.FunctionCall.Name,
                    Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        choice.Message.FunctionCall.Arguments, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new Dictionary<string, object>()
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OpenAI request");
            return new LLMProviderResponse 
            { 
                Success = false, 
                Error = "Internal error processing request" 
            };
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var testClient = new HttpClient();
            testClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            
            var response = await testClient.GetAsync("https://api.openai.com/v1/models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string MapModelId(string modelId)
    {
        return modelId switch
        {
            "gpt-35-turbo" => "gpt-3.5-turbo",
            "gpt-4" => "gpt-4",
            "gpt-4-turbo" => "gpt-4-turbo-preview",
            "gpt-5" => "gpt-5",
            "gpt-5-turbo" => "gpt-5-turbo",
            _ => "gpt-5"
        };
    }

    private List<OpenAIMessage> BuildMessages(LLMProviderRequest request)
    {
        var messages = new List<OpenAIMessage>();

        // Add system prompt
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new OpenAIMessage { Role = "system", Content = request.SystemPrompt });
        }

        // Add conversation history
        foreach (var msg in request.ConversationHistory)
        {
            messages.Add(new OpenAIMessage { Role = msg.Role, Content = msg.Content });
        }

        // Add current message
        messages.Add(new OpenAIMessage { Role = "user", Content = request.Message });

        return messages;
    }

    private List<OpenAIFunction>? MapFunctions(List<LLMFunction> functions)
    {
        if (!functions.Any()) return null;

        return functions.Select(f => new OpenAIFunction
        {
            Name = f.Name,
            Description = f.Description,
            Parameters = new OpenAIFunctionParameters
            {
                Type = "object",
                Properties = f.Parameters,
                Required = f.RequiredParameters
            }
        }).ToList();
    }

    private decimal CalculateCost(string modelId, OpenAIUsage? usage)
    {
        if (usage == null) return 0;

        var (inputCost, outputCost) = modelId switch
        {
            "gpt-35-turbo" => (0.0005m, 0.0015m),
            "gpt-4" => (0.03m, 0.06m),
            "gpt-4-turbo" => (0.01m, 0.03m),
            "gpt-5" => (0.05m, 0.10m),
            "gpt-5-turbo" => (0.02m, 0.04m),
            _ => (0.02m, 0.04m)
        };

        return (usage.PromptTokens / 1000m * inputCost) + (usage.CompletionTokens / 1000m * outputCost);
    }

    #region OpenAI API Models
    private class OpenAIRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<OpenAIMessage> Messages { get; set; } = new();
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public List<OpenAIFunction>? Functions { get; set; }
        public string? FunctionCall { get; set; }
    }

    private class OpenAIMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public OpenAIFunctionCall? FunctionCall { get; set; }
    }

    private class OpenAIFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OpenAIFunctionParameters Parameters { get; set; } = new();
    }

    private class OpenAIFunctionParameters
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Required { get; set; } = new();
    }

    private class OpenAIFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
    }

    private class OpenAIResponse
    {
        public List<OpenAIChoice> Choices { get; set; } = new();
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        public OpenAIMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class OpenAIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
    #endregion
}