// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.RegularExpressions;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;
using Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral;
using LLMFunction = Klacks.Api.Domain.Models.Assistant.LLMFunction;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic;

public class GenericOpenAICompatibleProvider : BaseHttpProvider
{
    public override string ProviderId => _providerConfig!.ProviderId;

    public override string ProviderName => _providerConfig!.ProviderName;

    private const string CodingAgentUserAgent = "claude-code/1.0";

    public GenericOpenAICompatibleProvider(HttpClient httpClient, ILogger<GenericOpenAICompatibleProvider> logger, IConfiguration configuration)
        : base(httpClient, logger)
    {
    }

    protected override void ConfigureHttpClient()
    {
        base.ConfigureHttpClient();

        if (_providerConfig?.BaseUrl?.Contains("api.kimi.com") == true)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CodingAgentUserAgent);
        }
    }

    public override async Task<LLMProviderResponse> ProcessAsync(LLMProviderRequest request)
    {
        if (!IsEnabled)
        {
            return CreateErrorResponse($"{ProviderName} provider is not enabled");
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return CreateErrorResponse("The provider for the selected model is not available.");
        }

        try
        {
            var openAiRequest = new MistralRequest
            {
                Model = request.ModelId,
                Messages = BuildMessages(request),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Tools = BuildTools(request.AvailableFunctions),
                ToolChoice = request.AvailableFunctions.Any() ? "auto" : null
            };

            var endpoint = "chat/completions";
            var response = await PostJsonAsync<MistralRequest, OpenAIResponse>(endpoint, openAiRequest);

            if (response?.Choices == null || !response.Choices.Any())
            {
                return CreateErrorResponse($"Invalid response from {ProviderName}");
            }

            var choice = response.Choices.First();
            var result = new LLMProviderResponse
            {
                Content = choice.Message?.Content ?? string.Empty,
                Success = true,
                Usage = new LLMUsage
                {
                    InputTokens = response.Usage?.PromptTokens ?? 0,
                    OutputTokens = response.Usage?.CompletionTokens ?? 0,
                    Cost = CalculateCost(request,
                        response.Usage?.PromptTokens ?? 0,
                        response.Usage?.CompletionTokens ?? 0)
                }
            };

            if (choice.Message?.ToolCalls != null && choice.Message.ToolCalls.Any())
            {
                foreach (var toolCall in choice.Message.ToolCalls)
                {
                    if (toolCall.Function != null)
                    {
                        result.FunctionCalls.Add(new LLMFunctionCall
                        {
                            FunctionName = toolCall.Function.Name,
                            Parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                                toolCall.Function.Arguments,
                                GetJsonSerializerOptions()
                            ) ?? new Dictionary<string, object>()
                        });
                    }
                }

                NormalizeMalformedParameters(result.FunctionCalls, request.AvailableFunctions);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} request: {Message}", ProviderName, ex.Message);
            return CreateErrorResponse($"Internal error processing request: {ex.Message}");
        }
    }

    public override async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(_httpClient.BaseAddress!, "models"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private List<OpenAIMessage> BuildMessages(LLMProviderRequest request)
    {
        var messages = new List<OpenAIMessage>();

        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            messages.Add(new OpenAIMessage { Role = "system", Content = request.SystemPrompt });
        }

        foreach (var msg in request.ConversationHistory)
        {
            messages.Add(new OpenAIMessage { Role = msg.Role, Content = msg.Content });
        }

        messages.Add(new OpenAIMessage { Role = "user", Content = request.Message });

        return messages;
    }

    private void NormalizeMalformedParameters(List<LLMFunctionCall> calls, List<LLMFunction> functions)
    {
        foreach (var call in calls)
        {
            var funcDef = functions.FirstOrDefault(f => f.Name == call.FunctionName);
            if (funcDef == null || funcDef.Parameters.Count == 0) continue;

            var expectedNames = funcDef.Parameters.Keys.ToList();

            if (call.Parameters.Keys.Any(k => expectedNames.Contains(k, StringComparer.OrdinalIgnoreCase)))
                continue;

            var rawValue = string.Join(", ", call.Parameters.Values.Select(v => v?.ToString() ?? ""));
            if (string.IsNullOrWhiteSpace(rawValue)) continue;

            var normalized = ExtractParametersFromString(rawValue, expectedNames);
            if (normalized.Count > 0)
            {
                _logger.LogInformation(
                    "Normalized malformed parameters for {Function}: {ParamCount} params extracted from raw string",
                    call.FunctionName, normalized.Count);
                call.Parameters = normalized;
            }
        }
    }

    private static readonly Dictionary<string, string[]> ParameterAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["firstName"] = ["Vorname", "First Name"],
        ["lastName"] = ["Nachname", "Last Name"],
        ["email"] = ["E-Mail", "Email-Adresse", "Mail"],
        ["userId"] = ["User-ID", "Benutzer-ID", "BenutzerID"],
        ["groupNames"] = ["Gruppenname", "Gruppennamen", "Group Name", "Group Names"],
        ["name"] = ["Name", "Bezeichnung"],
        ["script"] = ["Script", "Skript", "Code"],
    };

    private static Dictionary<string, object> ExtractParametersFromString(string raw, List<string> expectedNames)
    {
        var result = new Dictionary<string, object>();

        foreach (var paramName in expectedNames)
        {
            var patterns = new List<string> { Regex.Escape(paramName) };
            if (ParameterAliases.TryGetValue(paramName, out var aliases))
            {
                patterns.AddRange(aliases.Select(Regex.Escape));
            }

            var pattern = $@"(?:{string.Join("|", patterns)})\s*[:=]\s*(?:'([^']+)'|""([^""]+)""|(\S[^,\n]*))";
            var match = Regex.Match(raw, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var value = match.Groups[1].Success ? match.Groups[1].Value :
                            match.Groups[2].Success ? match.Groups[2].Value :
                            match.Groups[3].Value;
                result[paramName] = value.Trim();
            }
        }

        return result;
    }

    private List<MistralTool>? BuildTools(List<LLMFunction> functions)
    {
        if (!functions.Any())
        {
            return null;
        }

        return functions.Select(f => new MistralTool
        {
            Type = "function",
            Function = new MistralFunction
            {
                Name = f.Name,
                Description = f.Description,
                Parameters = new MistralFunctionParameters
                {
                    Type = "object",
                    Properties = f.Parameters,
                    Required = f.RequiredParameters.Any() ? f.RequiredParameters : null
                }
            }
        }).ToList();
    }
}
