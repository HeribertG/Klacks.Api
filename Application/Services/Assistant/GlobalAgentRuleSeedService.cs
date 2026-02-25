// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

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
            "Always respond in the language configured in the Klacks application settings. Check the current language setting and use it consistently for all responses.",
            2
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
        if (existingRules.Count > 0)
        {
            _logger.LogInformation("Global agent rules already exist ({Count} active). Skipping seed.", existingRules.Count);
            return;
        }

        var inserted = 0;
        foreach (var (name, content, sortOrder) in DefaultRules)
        {
            await _ruleRepository.UpsertRuleAsync(name, content, sortOrder, source: "seed", cancellationToken: cancellationToken);
            inserted++;
        }

        _logger.LogInformation("Seeded {Count} global agent rules.", inserted);
    }
}
