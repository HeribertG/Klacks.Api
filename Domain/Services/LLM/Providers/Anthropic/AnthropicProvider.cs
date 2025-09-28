using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers.Anthropic;

public class AnthropicProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicProvider> _logger;
    private readonly IConfiguration _configuration;
    private string _apiKey = string.Empty;
    private Models.LLM.LLMProvider? _providerConfig;

    public string ProviderId => _providerConfig?.ProviderId ?? "anthropic";
    public string ProviderName => _providerConfig?.ProviderName ?? "Anthropic";
    public bool IsEnabled => _providerConfig?.IsEnabled ?? false;

    public AnthropicProvider(HttpClient httpClient, ILogger<AnthropicProvider> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
    }

    public void Configure(Models.LLM.LLMProvider providerConfig)
    {
        _providerConfig = providerConfig;
        _apiKey = providerConfig.ApiKey ?? string.Empty;
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Remove("anthropic-version");
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", providerConfig.ApiVersion ?? "2024-01-01");
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
                Error = "Anthropic provider is not enabled" 
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
            var anthropicRequest = new AnthropicRequest
            {
                Model = request.ModelId,
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
                    Cost = CalculateCost(request, anthropicResponse.Usage)
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


    private List<AnthropicMessage> BuildMessages(LLMProviderRequest request)
    {
        var messages = new List<AnthropicMessage>();

        // Add conversation history
        foreach (var msg in request.ConversationHistory)
        {
            if (msg.Role != "system")
            {
                messages.Add(new AnthropicMessage { Role = msg.Role, Content = msg.Content });
            }
        }

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

    private decimal CalculateCost(LLMProviderRequest request, AnthropicUsage? usage)
    {
        if (usage == null) return 0;

        return (usage.InputTokens / 1000m * request.CostPerInputToken) + 
               (usage.OutputTokens / 1000m * request.CostPerOutputToken);
    }

}