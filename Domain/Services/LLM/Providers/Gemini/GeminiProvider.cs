using Klacks.Api.Domain.Services.LLM.Providers.Base;
using Klacks.Api.Presentation.DTOs.LLM;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Klacks.Api.Domain.Services.LLM.Providers.Gemini;

public class GeminiProvider : BaseHttpProvider
{
    private readonly IConfiguration _configuration;

    public override string ProviderId => "google";
    public override string ProviderName => "Google Gemini";

    public GeminiProvider(HttpClient httpClient, ILogger<GeminiProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    protected override void ConfigureHttpClient()
    {
    }

    public override async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request)
    {
        if (!IsEnabled)
        {
            return CreateErrorResponse("Google Gemini provider is not enabled");
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return CreateErrorResponse("Der Provider für das gewählte Modell ist nicht verfügbar.");
        }

        try
        {
            var modelName = request.ModelId;
            var endpoint = $"models/{modelName}:generateContent?key={_apiKey}";

            var geminiRequest = new GeminiRequest
            {
                Contents = BuildContents(request),
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = request.Temperature,
                    MaxOutputTokens = request.MaxTokens
                }
            };

            var geminiResponse = await PostJsonAsync<GeminiRequest, GeminiResponse>(endpoint, geminiRequest);

            if (geminiResponse?.Candidates == null || !geminiResponse.Candidates.Any())
            {
                _logger.LogError("Gemini response was null or had no candidates. Response: {Response}", 
                    geminiResponse != null ? System.Text.Json.JsonSerializer.Serialize(geminiResponse) : "null");
                return CreateErrorResponse("Invalid response from Gemini");
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
                    Cost = CalculateCost(request, 
                        geminiResponse.UsageMetadata?.PromptTokenCount ?? 0,
                        geminiResponse.UsageMetadata?.CandidatesTokenCount ?? 0)
                }
            };

            var functionCalls = ExtractFunctionCalls(candidate);
            result.FunctionCalls.AddRange(functionCalls);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Gemini request");
            return CreateErrorResponse("Internal error processing request");
        }
    }

    public override async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var response = await _httpClient.GetAsync($"models?key={apiKey}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private List<GeminiContent> BuildContents(LLMProviderRequest request)
    {
        var contents = new List<GeminiContent>();

        foreach (var msg in request.ConversationHistory)
        {
            if (msg.Role != "system")
            {
                contents.Add(new GeminiContent
                {
                    Role = msg.Role == "assistant" ? "model" : "user",
                    Parts = new[] { new GeminiPart { Text = msg.Content } }
                });
            }
        }

        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new[] { new GeminiPart { Text = request.Message } }
        });

        return contents;
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
}