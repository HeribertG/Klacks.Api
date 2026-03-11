// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seeds default global agent rules into the database if they are missing.
/// Uses upsert logic so new rules are added even when other rules already exist.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class GlobalAgentRuleSeedService
{
    private readonly IGlobalAgentRuleRepository _ruleRepository;
    private readonly ILogger<GlobalAgentRuleSeedService> _logger;

    private static readonly (string Name, string Content, int SortOrder)[] DefaultRules =
    [
        (
            "ADDRESS_VALIDATION",
            "Every address must be validated for existence before saving. Use the validate_address function to verify the address. Only save the address if it has been successfully verified.",
            0
        ),
        (
            "ADDRESS_COMPLETENESS",
            "When creating an address, always fill in the state and country fields whenever possible. Do not leave these fields empty if the information can be determined from the provided address data.",
            1
        ),
        (
            "RESPONSE_LANGUAGE",
            "You MUST respond in {{LANGUAGE}}. This is the configured application language. Use it consistently for all responses, regardless of what language the user writes in.",
            2
        ),
        (
            GlobalAgentRuleNames.SuggestionFormat,
            "At the end of every response, on a new line, append exactly 3 short follow-up suggestions the user might want to ask next. Use this exact format: [SUGGESTIONS: \"suggestion 1\" | \"suggestion 2\" | \"suggestion 3\"] — Keep each suggestion under 8 words. Do not add this line when the response is an error message.",
            3
        )
    ];

    public GlobalAgentRuleSeedService(
        IGlobalAgentRuleRepository ruleRepository,
        ILogger<GlobalAgentRuleSeedService> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingRules = await _ruleRepository.GetActiveRulesAsync(cancellationToken);
        var existingNames = existingRules.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        foreach (var (name, content, sortOrder) in DefaultRules)
        {
            if (existingNames.Contains(name))
                continue;

            await _ruleRepository.UpsertRuleAsync(name, content, sortOrder, source: "seed", cancellationToken: cancellationToken);
            inserted++;
        }

        if (inserted > 0)
            _logger.LogInformation("Seeded {Count} new global agent rules.", inserted);
        else
            _logger.LogInformation("All global agent rules already present. Nothing to seed.");
    }
}
