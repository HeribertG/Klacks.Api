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
/// <param name="navigationTargetCatalog">Route-scoped view of the in-page navigation target catalog, used to validate the optional 'target' parameter</param>
/// <param name="guidanceProviders">Optional per-page guidance sources consulted for entity navigations (first match wins)</param>
/// <param name="logger">Logger for non-fatal guidance lookup failures and skipped target validation</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("navigate_to")]
public partial class NavigateToSkill : BaseSkillImplementation
{
    private const int MaxTargetCandidatesInMessage = 20;

    private readonly IKlacksyPageKeyCatalog _pageKeyCatalog;
    private readonly INavigationTargetCatalog _navigationTargetCatalog;
    private readonly IEnumerable<INavigationGuidanceProvider> _guidanceProviders;
    private readonly ILogger<NavigateToSkill> _logger;

    public NavigateToSkill(
        IKlacksyPageKeyCatalog pageKeyCatalog,
        INavigationTargetCatalog navigationTargetCatalog,
        IEnumerable<INavigationGuidanceProvider> guidanceProviders,
        ILogger<NavigateToSkill> logger)
    {
        _pageKeyCatalog = pageKeyCatalog;
        _navigationTargetCatalog = navigationTargetCatalog;
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

        if (!string.IsNullOrEmpty(target))
        {
            var resolvedTarget = ResolveTarget(target, page, entry.Route);
            if (resolvedTarget.Error != null)
            {
                return SkillResult.Error(resolvedTarget.Error);
            }

            target = resolvedTarget.TargetId;
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

    private TargetResolution ResolveTarget(string target, string page, string route)
    {
        var routeTargets = _navigationTargetCatalog.GetByRoute(route);
        if (routeTargets == null || routeTargets.Count == 0)
        {
            _logger.LogWarning(
                "Navigation target validation skipped for page {Page} (route {Route}) — no targets are known for this route",
                page,
                route);
            return new TargetResolution(target, null);
        }

        var exactMatch = routeTargets.FirstOrDefault(
            t => string.Equals(t.TargetId, target, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return new TargetResolution(exactMatch.TargetId, null);
        }

        var normalizedTarget = NormalizeForComparison(target);
        var synonymMatches = routeTargets
            .Where(t => t.Synonyms.Values.Any(
                synonyms => synonyms.Any(synonym => NormalizeForComparison(synonym) == normalizedTarget)))
            .ToList();

        if (synonymMatches.Count == 1)
        {
            return new TargetResolution(synonymMatches[0].TargetId, null);
        }

        return new TargetResolution(null, BuildInvalidTargetMessage(target, page, routeTargets));
    }

    private static string BuildInvalidTargetMessage(
        string target, string page, IReadOnlyList<NavigationTargetEntry> routeTargets)
    {
        var candidateIds = routeTargets.Select(t => t.TargetId).ToList();
        var shown = candidateIds.Take(MaxTargetCandidatesInMessage).ToList();
        var omittedCount = candidateIds.Count - shown.Count;
        var suffix = omittedCount > 0 ? $", and {omittedCount} more" : string.Empty;

        return $"'{target}' is not a valid target for page '{page}'. Valid targets on this page: " +
               $"{string.Join(", ", shown)}{suffix}. Do not repeat this value or list internal target ids " +
               "to the user — pick one of these exact target ids for your next call, or omit 'target' to " +
               "navigate without an in-page scroll target.";
    }

    private static string NormalizeForComparison(string value)
    {
        var lowered = value.Trim().ToLowerInvariant().Replace('-', ' ');
        return WhitespaceRegex().Replace(lowered, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private readonly record struct TargetResolution(string? TargetId, string? Error);
}
