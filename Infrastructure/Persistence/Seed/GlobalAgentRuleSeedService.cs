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
            "UI MAP — element IDs for NAVIGATION and highlighting only.\n" +
            "CRITICAL: To create or change CLIENT/employee/customer data (name, address, phone, email, contract, group membership) you MUST use the data skills (create_employee, update_client, assign_contract_to_client, add_client_to_group). NEVER navigate to a client form and ask the user to fill it, and never fill client data fields via the UI. The Edit-Client IDs below are only for navigating to or highlighting a field, never for entering client data.\n" +
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
        ),
        (
            GlobalAgentRuleNames.GuidedWorkflow,
            "For complex tasks (creating/updating clients, assigning contracts/groups, configuring settings) use a GUIDED step-by-step workflow:\n" +
            "1. NAVIGATE FIRST: open the relevant page before asking questions.\n" +
            "2. ONE QUESTION AT A TIME: ask exactly one question per response and wait for the answer.\n" +
            "3. CREATE A CLIENT — collect: first and last name; gender; entity type (Employee, ExternEmp or Customer — these are the ONLY client types); birthdate; address (street, zip, city; derive the canton with lookup_location); email; phone. Email and phone are strongly recommended.\n" +
            "4. REAL DATA ONLY — never invent options. There is NO \"Minijob\" or any made-up employment/contract type. Offer contracts only from list_contracts(canton) and groups only from list_groups(canton); if a list is empty, say so plainly. The only client types are Employee/ExternEmp/Customer.\n" +
            "5. OFFER CHOICES via [REPLIES:single] using the REAL items found (e.g. exact contract names from list_contracts).\n" +
            "6. CONFIRM: summarize the collected data, then ask for confirmation.\n" +
            "7. COLLECT BEFORE CREATING: do NOT call create_employee until you have first+last name, gender, entity type, address, email AND phone. If email or phone is still missing, ask for it; only proceed without them if the user explicitly declines. Then EXECUTE IMMEDIATELY after confirmation: call create_employee with all collected fields including email, phone and entityType (it also creates the mandatory membership). Do NOT navigate to the form or ask the user to fill it in manually — YOU create the record by calling the function.\n" +
            "8. OPTIONAL LINKS: after creation offer to assign a contract (assign_contract_to_client, employees only) and add to a group (add_client_to_group), using only real contracts/groups.\n" +
            "9. REVIEW: after create_employee (and after update_client) call navigate_to with page 'edit-employee' and entityId set to the returned client id, then ask the user to review and verify the data.\n" +
            "10. UPDATE A CLIENT: for ANY request to change or add a phone, email, address or master field of an existing employee/customer, your FIRST and ONLY action is to CALL update_client — pass firstName+lastName to identify the client (do NOT search first, do NOT navigate first, do NOT use search_and_navigate or navigate_to, do NOT ask the user to fill the form). Supplying street/zip/city adds a new address; email or phone adds a new communication. ONLY AFTER update_client returns success, navigate to the client (step 9) for review. If you navigated or asked the user to edit instead of calling update_client, you did it WRONG.",
            6
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
