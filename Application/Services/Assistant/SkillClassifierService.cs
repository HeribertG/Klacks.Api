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

    private const int ClassificationTimeoutMs = 1500;
    private const double ClassificationTemperature = 0.1;
    private const int ClassificationMaxTokens = 50;

    private const string SystemPrompt =
        "You are a domain classifier for a workforce management app. " +
        "Respond with only lowercase German domain keywords from this list, comma-separated, no explanation:\n" +
        "mitarbeiter (employees, staff, team, contacts, phone list, supervisor), " +
        "email (email settings, SMTP, IMAP), " +
        "kalender (calendar, holidays, appointments, meetings, scheduling dates), " +
        "filiale (branches, offices, locations, opening hours), " +
        "benutzer (user accounts, login, permissions), " +
        "vertrag (contracts, employment agreements), " +
        "gruppe (teams, departments, groups), " +
        "einstellung (app settings, configuration), " +
        "llm (AI models, providers, API keys), " +
        "skill (chatbot skills, functions, capabilities), " +
        "erinnerung (AI memory, notes, remember preferences), " +
        "richtlinie (AI guidelines, behavioral rules), " +
        "seele (chatbot personality configuration ONLY — NOT philosophy or life questions), " +
        "dienstplan (shift planning, rosters, schedules, tasks, work assignments, duty), " +
        "suche (search, find, lookup), " +
        "adresse (postal addresses, geocoding, street validation), " +
        "spam (spam filter settings), " +
        "heartbeat (system monitoring, health checks), " +
        "web (web search settings).\n" +
        "If the message is general conversation, jokes, philosophy, math, greetings, or has no relation to any domain above, respond with: none";

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
