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
        ),
        (
            GlobalAgentRuleNames.UiElementMap,
            "UI MAP — use element IDs with navigate_to/UiAction.\n" +
            "ROUTES: /workplace/dashboard|client|edit-address/:id|schedule|absence|shift|edit-shift/:id|cut-shift/:id|container-template/:id|group|edit-group/:id|settings|profile|floor-plan|inbox\n" +
            "IDs: Client List: new-address-button,myAddressTable,filter-reset-button | " +
            "Edit Client: firstname,profile-name,company,gender,street,zip,city,state,country,client-type,add-contract-button,add-group-button | " +
            "Schedule: schedule-prev-btn,schedule-next-btn,schedule-wizard-btn,schedule-pdf-export-btn,schedule-recalculate-btn | " +
            "Shifts: shift-create-btn,abbreviation,name,validFrom,isMonday-isSunday,sumEmployees | " +
            "Cut: cut-date-btn,cut-time-btn,cut-weekdays-btn,cut-staff-btn,reset-cuts-btn | " +
            "Groups: all-group-list-new-button,all-group-list-tree-toggle,edit-group-item-name | " +
            "Settings: setting-general-name,setting-owner-address-name,setting-email-test-btn,setting-imap-test-btn,contractName,absence-modal-name,deepl-apikey",
            4
        ),
        (
            GlobalAgentRuleNames.SuggestedRepliesFormat,
            "When you need the user to choose from options, append a REPLIES block.\n" +
            "Single-select: [REPLIES:single \"Option A\" | \"Option B\" | \"Option C\"]\n" +
            "Multi-select with heading: [REPLIES:multi:Choose items \"Label1=value1\" | \"Label2=value2\"]\n" +
            "Rules: Use Label=Value for differing display/data. Max 10 options. Do not combine with SUGGESTIONS in the same response.",
            5
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
