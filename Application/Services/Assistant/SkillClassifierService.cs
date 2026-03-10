// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Tier 2 skill classifier that calls a lightweight LLM to identify domain keywords
/// from a user message when Tier 1 keyword matching yields no results.
/// Falls back to an empty list on any error or timeout.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillClassifierService : ISkillClassifierService
{
    private readonly LLMProviderOrchestrator _orchestrator;
    private readonly ILogger<SkillClassifierService> _logger;

    private const int ClassificationTimeoutMs = 3000;
    private const double ClassificationTemperature = 0.1;
    private const int ClassificationMaxTokens = 50;

    private const string SystemPrompt =
        "You are a domain classifier. Respond with only lowercase German domain keywords from this list, " +
        "comma-separated, no explanation: " +
        "mitarbeiter, email, kalender, filiale, benutzer, vertrag, gruppe, einstellung, llm, skill, " +
        "erinnerung, richtlinie, seele, dienstplan, suche, adresse, spam, heartbeat, web. " +
        "If the message has no relation to any domain, respond with: none";

    public SkillClassifierService(
        LLMProviderOrchestrator orchestrator,
        ILogger<SkillClassifierService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<List<string>> ClassifyMessageAsync(
        string message,
        string? language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ClassificationTimeoutMs);

            var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(null);

            if (model == null || provider == null)
            {
                _logger.LogDebug("Skill classifier: no model/provider available. {Error}", error);
                return [];
            }

            var request = new LLMProviderRequest
            {
                Message = message,
                SystemPrompt = SystemPrompt,
                ModelId = model.ModelId,
                Temperature = ClassificationTemperature,
                MaxTokens = ClassificationMaxTokens,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken
            };

            var response = await provider.ProcessAsync(request);

            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
                return [];

            return ParseClassificationResponse(response.Content);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Skill classifier timed out after {Timeout}ms", ClassificationTimeoutMs);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Skill classifier encountered an error");
            return [];
        }
    }

    private static List<string> ParseClassificationResponse(string content)
    {
        var trimmed = content.Trim().ToLowerInvariant();

        if (trimmed == "none" || trimmed.StartsWith("none"))
            return [];

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();
    }
}
