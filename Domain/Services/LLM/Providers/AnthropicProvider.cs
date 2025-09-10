using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers;

public class AnthropicProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicProvider> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _apiKey;

    public string ProviderId => "anthropic";
    public string ProviderName => "Anthropic";
    public bool IsEnabled { get; private set; }

    public AnthropicProvider(HttpClient httpClient, ILogger<AnthropicProvider> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        _apiKey = _configuration["LLM:Anthropic:ApiKey"] ?? string.Empty;
        IsEnabled = !string.IsNullOrEmpty(_apiKey) && _configuration.GetValue<bool>("LLM:Anthropic:Enabled", false);

        if (IsEnabled)
        {
            _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2024-01-01");
        }
    }

    public async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request)
    {
        if (!IsEnabled)
        {
            return new LLMProviderResponse 
            { 
                Success = false, 
                Error = "Anthropic provider is not enabled" 
            };
        }

        try
        {
            var anthropicRequest = new AnthropicRequest
            {
                Model = MapModelId(request.ModelId),
                Messages = BuildMessages(request),
                System = request.SystemPrompt,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Tools = MapTools(request.AvailableFunctions)
            };

            var json = JsonSerializer.Serialize(anthropicRequest, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("messages", requestContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Anthropic API error: {StatusCode} - {Error}", response.StatusCode, error);
                return new LLMProviderResponse 
                { 
                    Success = false, 
                    Error = $"Anthropic API error: {response.StatusCode}" 
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (anthropicResponse == null)
            {
                return new LLMProviderResponse 
                { 
                    Success = false, 
                    Error = "Invalid response from Anthropic" 
                };
            }

            var result = new LLMProviderResponse
            {
                Content = ExtractContent(anthropicResponse),
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = anthropicResponse.Usage?.InputTokens ?? 0,
                    OutputTokens = anthropicResponse.Usage?.OutputTokens ?? 0,
                    Cost = CalculateCost(request.ModelId, anthropicResponse.Usage)
                }
            };

            // Handle tool calls if any
            if (anthropicResponse.Content != null)
            {
                foreach (var content in anthropicResponse.Content)
                {
                    if (content.Type == "tool_use" && content.Name != null)
                    {
                        result.FunctionCalls.Add(new LLMFunctionCall
                        {
                            FunctionName = content.Name,
                            Parameters = content.Input ?? new Dictionary<string, object>()
                        });
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Anthropic request");
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
            testClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            testClient.DefaultRequestHeaders.Add("anthropic-version", "2024-01-01");
            
            var testRequest = new
            {
                model = "claude-3-haiku-20240307",
                messages = new[] { new { role = "user", content = "Hi" } },
                max_tokens = 10
            };
            
            var content = new StringContent(JsonSerializer.Serialize(testRequest), Encoding.UTF8, "application/json");
            var response = await testClient.PostAsync("https://api.anthropic.com/v1/messages", content);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.PaymentRequired;
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
            "claude-3-haiku" => "claude-3-haiku-20240307",
            "claude-3-sonnet" => "claude-3-5-sonnet-20241022",
            "claude-3-opus" => "claude-3-opus-20240229",
            "claude-37-haiku" => "claude-3.7-haiku-20250115",
            "claude-37-sonnet" => "claude-3.7-sonnet-20250115", 
            "claude-37-opus" => "claude-3.7-opus-20250115",
            _ => "claude-3.7-sonnet-20250115"
        };
    }

    private List<AnthropicMessage> BuildMessages(LLMProviderRequest request)
    {
        var messages = new List<AnthropicMessage>();

        // Add conversation history
        foreach (var msg in request.ConversationHistory)
        {
            if (msg.Role != "system") // System messages are handled separately in Anthropic
            {
                messages.Add(new AnthropicMessage { Role = msg.Role, Content = msg.Content });
            }
        }

        // Add current message
        messages.Add(new AnthropicMessage { Role = "user", Content = request.Message });

        return messages;
    }

    private List<AnthropicTool>? MapTools(List<LLMFunction> functions)
    {
        if (!functions.Any()) return null;

        return functions.Select(f => new AnthropicTool
        {
            Name = f.Name,
            Description = f.Description,
            InputSchema = new AnthropicInputSchema
            {
                Type = "object",
                Properties = f.Parameters,
                Required = f.RequiredParameters
            }
        }).ToList();
    }

    private string ExtractContent(AnthropicResponse response)
    {
        if (response.Content == null || !response.Content.Any())
            return string.Empty;

        var textContents = response.Content
            .Where(c => c.Type == "text")
            .Select(c => c.Text ?? string.Empty);

        return string.Join("\n", textContents);
    }

    private decimal CalculateCost(string modelId, AnthropicUsage? usage)
    {
        if (usage == null) return 0;

        var (inputCost, outputCost) = modelId switch
        {
            "claude-3-haiku" => (0.00025m, 0.00125m),
            "claude-3-sonnet" => (0.003m, 0.015m),
            "claude-3-opus" => (0.015m, 0.075m),
            "claude-37-haiku" => (0.0002m, 0.001m),
            "claude-37-sonnet" => (0.0025m, 0.012m),
            "claude-37-opus" => (0.012m, 0.06m),
            _ => (0.0025m, 0.012m)
        };

        return (usage.InputTokens / 1000m * inputCost) + (usage.OutputTokens / 1000m * outputCost);
    }

    #region Anthropic API Models
    private class AnthropicRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<AnthropicMessage> Messages { get; set; } = new();
        public string? System { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public List<AnthropicTool>? Tools { get; set; }
    }

    private class AnthropicMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class AnthropicTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AnthropicInputSchema InputSchema { get; set; } = new();
    }

    private class AnthropicInputSchema
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Required { get; set; } = new();
    }

    private class AnthropicResponse
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Role { get; set; }
        public List<AnthropicContent>? Content { get; set; }
        public AnthropicUsage? Usage { get; set; }
    }

    private class AnthropicContent
    {
        public string Type { get; set; } = string.Empty;
        public string? Text { get; set; }
        public string? Name { get; set; }
        public Dictionary<string, object>? Input { get; set; }
    }

    private class AnthropicUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
    #endregion
}