using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM.Providers;

public class GeminiProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiProvider> _logger;
    private readonly IConfiguration _configuration;
    private string _apiKey = string.Empty;
    private Models.LLM.LLMProvider? _providerConfig;

    public string ProviderId => "google";
    public string ProviderName => "Google Gemini";
    public bool IsEnabled => _providerConfig?.IsEnabled ?? false;

    public GeminiProvider(HttpClient httpClient, ILogger<GeminiProvider> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
    }

    public void Configure(Models.LLM.LLMProvider providerConfig)
    {
        _providerConfig = providerConfig;
        _apiKey = providerConfig.ApiKey ?? string.Empty;
        
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
                Error = "Google Gemini provider is not enabled" 
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
            var modelName = MapModelId(request.ModelId);
            var endpoint = $"models/{modelName}:generateContent?key={_apiKey}";

            var geminiRequest = new GeminiRequest
            {
                Contents = BuildContents(request),
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = request.Temperature,
                    MaxOutputTokens = request.MaxTokens
                },
                Tools = MapTools(request.AvailableFunctions)
            };

            // Add system instruction if provided
            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                geminiRequest.SystemInstruction = new GeminiContent
                {
                    Parts = new[] { new GeminiPart { Text = request.SystemPrompt } }
                };
            }

            var json = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, error);
                return new LLMProviderResponse 
                { 
                    Success = false, 
                    Error = $"Gemini API error: {response.StatusCode}" 
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (geminiResponse?.Candidates == null || !geminiResponse.Candidates.Any())
            {
                return new LLMProviderResponse 
                { 
                    Success = false, 
                    Error = "Invalid response from Gemini" 
                };
            }

            var candidate = geminiResponse.Candidates[0];
            var result = new LLMProviderResponse
            {
                Content = ExtractTextContent(candidate),
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = geminiResponse.UsageMetadata?.PromptTokenCount ?? 0,
                    OutputTokens = geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
                    Cost = CalculateCost(request.ModelId, geminiResponse.UsageMetadata)
                }
            };

            // Handle function calls if any
            var functionCalls = ExtractFunctionCalls(candidate);
            result.FunctionCalls.AddRange(functionCalls);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Gemini request");
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
            var response = await testClient.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");
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
            "gemini-15-flash" => "gemini-1.5-flash-latest",
            "gemini-15-pro" => "gemini-1.5-pro-latest",
            "gemini-20-flash" => "gemini-2.0-flash-exp",
            "gemini-25-flash" => "gemini-2.5-flash",
            "gemini-25-pro" => "gemini-2.5-pro",
            "gemini-25-ultra" => "gemini-2.5-ultra",
            _ => "gemini-2.5-pro"
        };
    }

    private List<GeminiContent> BuildContents(LLMProviderRequest request)
    {
        var contents = new List<GeminiContent>();

        // Add conversation history
        foreach (var msg in request.ConversationHistory)
        {
            if (msg.Role != "system") // System is handled separately
            {
                contents.Add(new GeminiContent
                {
                    Role = msg.Role == "assistant" ? "model" : "user",
                    Parts = new[] { new GeminiPart { Text = msg.Content } }
                });
            }
        }

        // Add current message
        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new[] { new GeminiPart { Text = request.Message } }
        });

        return contents;
    }

    private List<GeminiTool>? MapTools(List<LLMFunction> functions)
    {
        if (!functions.Any()) return null;

        var functionDeclarations = functions.Select(f => new GeminiFunctionDeclaration
        {
            Name = f.Name,
            Description = f.Description,
            Parameters = new GeminiParameters
            {
                Type = "object",
                Properties = f.Parameters,
                Required = f.RequiredParameters
            }
        }).ToList();

        return new List<GeminiTool>
        {
            new GeminiTool { FunctionDeclarations = functionDeclarations }
        };
    }

    private string ExtractTextContent(GeminiCandidate candidate)
    {
        if (candidate.Content?.Parts == null)
            return string.Empty;

        var textParts = candidate.Content.Parts
            .Where(p => !string.IsNullOrEmpty(p.Text))
            .Select(p => p.Text!);

        return string.Join("\n", textParts);
    }

    private List<LLMFunctionCall> ExtractFunctionCalls(GeminiCandidate candidate)
    {
        var functionCalls = new List<LLMFunctionCall>();

        if (candidate.Content?.Parts != null)
        {
            foreach (var part in candidate.Content.Parts)
            {
                if (part.FunctionCall != null)
                {
                    functionCalls.Add(new LLMFunctionCall
                    {
                        FunctionName = part.FunctionCall.Name,
                        Parameters = part.FunctionCall.Args ?? new Dictionary<string, object>()
                    });
                }
            }
        }

        return functionCalls;
    }

    private decimal CalculateCost(string modelId, GeminiUsageMetadata? usage)
    {
        if (usage == null) return 0;

        var (inputCost, outputCost) = modelId switch
        {
            "gemini-15-flash" => (0.00035m, 0.0007m),
            "gemini-15-pro" => (0.0035m, 0.0105m),
            "gemini-20-flash" => (0.0003m, 0.0006m),
            "gemini-25-flash" => (0.00025m, 0.0005m),
            "gemini-25-pro" => (0.0025m, 0.0075m),
            "gemini-25-ultra" => (0.005m, 0.015m),
            _ => (0.0025m, 0.0075m)
        };

        return (usage.PromptTokenCount / 1000m * inputCost) + (usage.CandidatesTokenCount / 1000m * outputCost);
    }

    #region Gemini API Models
    private class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = new();
        public GeminiGenerationConfig? GenerationConfig { get; set; }
        public GeminiContent? SystemInstruction { get; set; }
        public List<GeminiTool>? Tools { get; set; }
    }

    private class GeminiContent
    {
        public string? Role { get; set; }
        public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
        public GeminiFunctionCall? FunctionCall { get; set; }
    }

    private class GeminiFunctionCall
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object>? Args { get; set; }
    }

    private class GeminiGenerationConfig
    {
        public double Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
    }

    private class GeminiTool
    {
        public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
    }

    private class GeminiFunctionDeclaration
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GeminiParameters Parameters { get; set; } = new();
    }

    private class GeminiParameters
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Required { get; set; } = new();
    }

    private class GeminiResponse
    {
        public List<GeminiCandidate> Candidates { get; set; } = new();
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
    }

    private class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
    }
    #endregion
}