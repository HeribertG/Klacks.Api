// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that navigates to a known page in the Klacks application by page key
/// (e.g. dashboard, settings, schedule). Routes and per-page permission
/// requirements come from the auto-generated klacksy-page-keys.generated.json
/// which the Klacks.Ui scanner produces from a single TypeScript source file —
/// so Angular routes, UI fallback map, and this skill cannot drift apart.
/// Permission filtering uses the executing user's claim list to refuse pages
/// they may not enter; the UI router guards remain a second line of defence.
/// For searching a person or entity by name, use search_and_navigate instead.
/// Entity-aware pages may carry an extra guidance note (e.g. sealed-shift lock state)
/// contributed by the first matching guidance provider; a guidance failure never
/// blocks the navigation itself.
/// </summary>
/// <param name="pageKeyCatalog">Singleton catalog loaded once from the generated JSON</param>
/// <param name="guidanceProviders">Optional per-page guidance sources consulted for entity navigations (first match wins)</param>
/// <param name="logger">Logger for non-fatal guidance lookup failures</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("navigate_to")]
public class NavigateToSkill : BaseSkillImplementation
{
    private readonly IKlacksyPageKeyCatalog _pageKeyCatalog;
    private readonly IEnumerable<INavigationGuidanceProvider> _guidanceProviders;
    private readonly ILogger<NavigateToSkill> _logger;

    public NavigateToSkill(
        IKlacksyPageKeyCatalog pageKeyCatalog,
        IEnumerable<INavigationGuidanceProvider> guidanceProviders,
        ILogger<NavigateToSkill> logger)
    {
        _pageKeyCatalog = pageKeyCatalog;
        _guidanceProviders = guidanceProviders;
        _logger = logger;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var page = GetRequiredString(parameters, "page");
        var entityId = GetParameter<string>(parameters, "entityId");
        var tab = GetParameter<string>(parameters, "tab");
        var target = GetParameter<string>(parameters, "target");

        // Runs before catalog resolution so it also covers the 'edit-address' route alias
        // (not a catalog page key) and gives a create_employee-specific hint; the generic
        // HasEntityParam guard below catches all other entity editors.
        if (string.IsNullOrEmpty(entityId)
            && (page.Equals(UiPageKeys.EditEmployee, StringComparison.OrdinalIgnoreCase)
                || page.Equals(UiPageKeys.EditAddress, StringComparison.OrdinalIgnoreCase)))
        {
            return SkillResult.Error(
                "Refusing to open the client editor without a client id — there is nothing to show yet. " +
                "To create a new client, call create_employee; only navigate here afterwards with the entityId of the created client.");
        }

        var entry = _pageKeyCatalog.GetByPageKey(page);
        if (entry == null)
        {
            return SkillResult.Error(
                $"'{page}' is not a recognized page key ({_pageKeyCatalog.AllPageKeys.Count()} pages are defined). " +
                "Do not repeat this value or list internal page keys to the user — ask them, in plain business " +
                "language, what page or feature they mean.");
        }

        if (string.IsNullOrEmpty(entityId) && entry.HasEntityParam)
        {
            return SkillResult.Error(
                $"Refusing to open '{page}' without an entity id — there is nothing to edit yet. " +
                "Identify the record first (use search_and_navigate to find it by name, or create it), " +
                "then navigate here with its entityId.");
        }

        if (!string.IsNullOrEmpty(entityId) && entry.HasEntityParam && !Guid.TryParse(entityId, out _))
        {
            return SkillResult.Error(
                $"'{entityId}' is not a valid entity id for '{page}' — it must be the internal GUID id " +
                "returned by search_and_navigate or create_employee, never a human-readable ID number or " +
                "any other value shown to the user. Call search_and_navigate to resolve the correct id first.");
        }

        if (!string.IsNullOrEmpty(entry.RequiredPermission)
            && !Permissions.HasPermission(context.UserPermissions, entry.RequiredPermission))
        {
            return SkillResult.Error(
                $"User '{context.UserName}' is not allowed to open page '{page}' (requires permission '{entry.RequiredPermission}').");
        }

        var route = entry.Route;
        if (!string.IsNullOrEmpty(entityId) && entry.HasEntityParam)
        {
            route += $"/{entityId}";
        }

        var queryParams = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(tab))
        {
            queryParams["tab"] = tab;
        }

        var navigationData = new
        {
            Page = page,
            Route = route,
            EntityId = entityId,
            Tab = tab,
            Target = target,
            QueryParams = queryParams
        };

        var message = await AppendGuidanceAsync($"Navigate to {page}", entry, page, entityId, cancellationToken);
        return SkillResult.Navigation(navigationData, message);
    }

    private async Task<string> AppendGuidanceAsync(
        string message,
        KlacksyPageKeyEntry entry,
        string page,
        string? entityId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entityId) || !entry.HasEntityParam || !Guid.TryParse(entityId, out var parsedEntityId))
        {
            return message;
        }

        var provider = _guidanceProviders.FirstOrDefault(p => p.CanHandle(page));
        if (provider == null)
        {
            return message;
        }

        try
        {
            var guidance = await provider.GetGuidanceAsync(page, parsedEntityId, cancellationToken);
            if (!string.IsNullOrEmpty(guidance))
            {
                return message + " " + guidance;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation guidance lookup failed for page {Page}", page);
        }

        return message;
    }
}
