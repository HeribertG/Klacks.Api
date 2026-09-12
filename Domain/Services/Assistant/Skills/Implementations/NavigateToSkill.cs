// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that navigates to a known page in the Klacks application by page key
/// (e.g. dashboard, settings, schedule). Routes and per-page permission
/// requirements come from the auto-generated klacksy-page-keys.generated.json
/// which the Klacks.Ui scanner produces from a single TypeScript source file —
/// so Angular routes, UI fallback map, and this skill cannot drift apart.
/// Pages contributed by installed feature plugins are not in that manifest and are
/// resolved from the plugin route catalog as a second lookup step.
/// Permission filtering uses the executing user's claim list to refuse pages
/// they may not enter, and the same helper refuses an in-page target the user may not
/// reach — a refused target opens the page without scrolling instead of failing the
/// navigation, since the page-level check has already allowed the page itself.
/// A page whose manifest entry names a required feature is refused on installations that do not have
/// that feature - a plugin that is not installed and enabled, or an inbox without an incoming mail
/// server - because its router guard refuses it there too; that refusal is about the feature, not
/// about rights, and is worded as such.
/// Target validation is fail-closed: a route the target catalog holds no entry for drops the target
/// instead of forwarding the caller's raw string to the frontend. Plugin pages are the likely case,
/// because only their sidebar nav button is scanned, never their page content.
/// The UI router guards remain a second line of defence.
/// For searching a person or entity by name, use search_and_navigate instead.
/// Entity-aware pages may carry an extra guidance note (e.g. sealed-shift lock state)
/// contributed by the first matching guidance provider; a guidance failure never
/// blocks the navigation itself.
/// </summary>
/// <param name="pageKeyCatalog">Singleton catalog loaded once from the generated JSON</param>
/// <param name="navigationTargetCatalog">Route-scoped view of the in-page navigation target catalog, used to validate the optional 'target' parameter</param>
/// <param name="pluginRouteCatalog">Routes installed feature plugins registered for themselves, consulted when the page key is unknown to the generated manifest</param>
/// <param name="featureAvailability">Tells whether an optional feature exists on this installation, mirroring the feature route guards</param>
/// <param name="guidanceProviders">Optional per-page guidance sources consulted for entity navigations (first match wins)</param>
/// <param name="logger">Logger for non-fatal guidance lookup failures and discarded in-page targets</param>

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
    private const string PageScope = "this page";
    private const string SectionScope = "this section";

    private const string NoInternalIdentifiersGuidance =
        " Tell the user, in plain business language, that this is not available to them — never say it " +
        "could not be found, and do not name internal identifiers such as page keys, target ids or " +
        "permission names.";

    private const string FeatureNotEnabledText =
        "That part of Klacks is not enabled on this installation, so there is no page to open. Tell the " +
        "user, in plain business language, that this installation does not have that feature set up — not " +
        "that they lack a right and not that anything could not be found — and do not name internal " +
        "identifiers such as page keys, feature names, setting names or server details.";

    private const string UnknownRouteTargetNote =
        "No in-page section is known for this page, so it was opened without scrolling to one. Do not " +
        "repeat the requested section name, and do not name internal identifiers such as target ids, " +
        "page keys or permission names — tell the user, in plain business language, that the page is " +
        "open and they may have to look for that part of it themselves.";

    private readonly IKlacksyPageKeyCatalog _pageKeyCatalog;
    private readonly INavigationTargetCatalog _navigationTargetCatalog;
    private readonly IPluginNavigationRouteCatalog _pluginRouteCatalog;
    private readonly IFeatureAvailabilityService _featureAvailability;
    private readonly IEnumerable<INavigationGuidanceProvider> _guidanceProviders;
    private readonly ILogger<NavigateToSkill> _logger;

    public NavigateToSkill(
        IKlacksyPageKeyCatalog pageKeyCatalog,
        INavigationTargetCatalog navigationTargetCatalog,
        IPluginNavigationRouteCatalog pluginRouteCatalog,
        IFeatureAvailabilityService featureAvailability,
        IEnumerable<INavigationGuidanceProvider> guidanceProviders,
        ILogger<NavigateToSkill> logger)
    {
        _pageKeyCatalog = pageKeyCatalog;
        _navigationTargetCatalog = navigationTargetCatalog;
        _pluginRouteCatalog = pluginRouteCatalog;
        _featureAvailability = featureAvailability;
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

        var entry = _pageKeyCatalog.GetByPageKey(page) ?? ResolvePluginPage(page);
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

        if (!Permissions.HasAllRequiredPermissions(context.UserPermissions, entry.RequiredPermission))
        {
            return SkillResult.Error(BuildPermissionDeniedText(context.UserName, PageScope));
        }

        if (entry.RequiredFeature != null
            && !await _featureAvailability.IsAvailableAsync(entry.RequiredFeature, cancellationToken))
        {
            return SkillResult.Error(FeatureNotEnabledText);
        }

        string? targetDeniedNote = null;
        if (!string.IsNullOrEmpty(target))
        {
            var resolvedTarget = ResolveTarget(target, page, entry.Route, context);
            if (resolvedTarget.Error != null)
            {
                return SkillResult.Error(resolvedTarget.Error);
            }

            if (resolvedTarget.TargetId != null
                && !await IsTargetFeatureAvailableAsync(resolvedTarget, entry, cancellationToken))
            {
                return SkillResult.Error(FeatureNotEnabledText);
            }

            target = resolvedTarget.TargetId;
            targetDeniedNote = resolvedTarget.DeniedNote;
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
        if (targetDeniedNote != null)
        {
            message += " " + targetDeniedNote;
        }

        return SkillResult.Navigation(navigationData, message);
    }

    private static string BuildPermissionDeniedText(string userName, string scope)
        => $"User '{userName}' is not allowed to open {scope}." + NoInternalIdentifiersGuidance;

    /// <summary>
    /// Second feature gate, for the in-page target. In every case reachable today it is a no-op: a
    /// target inherits the feature of the page-key of its own route, so the page check above has
    /// already asked the same question, and the fast-path cache drops targets of unavailable features
    /// from its snapshot entirely. It is not dead in general — a page resolved through the plugin route
    /// fallback carries no feature of its own, and that fallback reads a best-effort catalogue that can
    /// lag a disable — so a target naming a different feature is asked about rather than trusted.
    /// </summary>
    /// <param name="resolvedTarget">The target already resolved and allowed by permission</param>
    /// <param name="entry">The page entry whose feature was checked before</param>
    /// <param name="cancellationToken">Cancels the availability lookup</param>
    private async Task<bool> IsTargetFeatureAvailableAsync(
        TargetResolution resolvedTarget,
        KlacksyPageKeyEntry entry,
        CancellationToken cancellationToken)
    {
        if (resolvedTarget.RequiredFeature == null
            || string.Equals(resolvedTarget.RequiredFeature, entry.RequiredFeature, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return await _featureAvailability.IsAvailableAsync(resolvedTarget.RequiredFeature, cancellationToken);
    }

    /// <summary>
    /// Second lookup step for pages an installed feature plugin brought with it. Those routes live in
    /// the navigate_to skill's HandlerConfig, not in the scanner-generated page-key manifest, so the
    /// catalog above cannot know them. No permission is attached: a plugin page is reachable by every
    /// logged-in user once the feature is installed, which is exactly how the messaging plugin behaves
    /// today. Entity ids are not supported — a plugin registers one flat route.
    /// </summary>
    /// <param name="page">Page key the caller passed, already rejected by the page-key catalog</param>
    private KlacksyPageKeyEntry? ResolvePluginPage(string page)
    {
        var route = _pluginRouteCatalog.GetRoute(page);
        return route == null ? null : new KlacksyPageKeyEntry(page, route, null, false);
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

    private TargetResolution ResolveTarget(string target, string page, string route, SkillExecutionContext context)
    {
        var routeTargets = _navigationTargetCatalog.GetByRoute(route);
        if (routeTargets == null || routeTargets.Count == 0)
        {
            _logger.LogWarning(
                "Navigation target discarded for page {Page} (route {Route}) — no targets are known for this route",
                page,
                route);
            return new TargetResolution(null, null, UnknownRouteTargetNote);
        }

        var exactMatch = routeTargets.FirstOrDefault(
            t => string.Equals(t.TargetId, target, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
        {
            return ResolveAllowedTarget(exactMatch, context);
        }

        var normalizedTarget = NormalizeForComparison(target);
        var synonymMatches = routeTargets
            .Where(t => t.Synonyms.Values.Any(
                synonyms => synonyms.Any(synonym => NormalizeForComparison(synonym) == normalizedTarget)))
            .ToList();

        if (synonymMatches.Count == 1)
        {
            return ResolveAllowedTarget(synonymMatches[0], context);
        }

        return new TargetResolution(null, BuildInvalidTargetMessage(target, page, routeTargets));
    }

    /// <summary>
    /// Applies the same permission helper the chat fast-path uses (NavigationTargetMatcher.IsAllowed).
    /// A refused target does NOT refuse the navigation: the page-level check above already passed, so
    /// the page itself is open to this user and blocking it here would make the assistant stricter than
    /// a mouse click. The page is opened without an in-page target and the caller is told, plainly, that
    /// the section is out of reach — never that it does not exist. The note names no target id, page key
    /// or permission, so the model cannot leak an internal identifier it was handed here.
    /// </summary>
    /// <param name="entry">The target the user asked for, already resolved by id or synonym</param>
    /// <param name="context">Execution context supplying the caller's rights and name</param>
    private static TargetResolution ResolveAllowedTarget(
        NavigationTargetEntry entry, SkillExecutionContext context)
    {
        if (Permissions.HasAllRequiredPermissions(context.UserPermissions, entry.RequiredPermission))
        {
            return new TargetResolution(entry.TargetId, null, RequiredFeature: entry.RequiredFeature);
        }

        var note = BuildPermissionDeniedText(context.UserName, SectionScope) +
                   " The page itself was opened, without scrolling to that section.";

        return new TargetResolution(null, null, note);
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

    private readonly record struct TargetResolution(
        string? TargetId, string? Error, string? DeniedNote = null, string? RequiredFeature = null);
}
