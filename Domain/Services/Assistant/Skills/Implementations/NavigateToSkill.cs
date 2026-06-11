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
/// </summary>
/// <param name="pageKeyCatalog">Singleton catalog loaded once from the generated JSON</param>
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("navigate_to")]
public class NavigateToSkill : BaseSkillImplementation
{
    private readonly IKlacksyPageKeyCatalog _pageKeyCatalog;

    public NavigateToSkill(IKlacksyPageKeyCatalog pageKeyCatalog)
    {
        _pageKeyCatalog = pageKeyCatalog;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var page = GetRequiredString(parameters, "page");
        var entityId = GetParameter<string>(parameters, "entityId");
        var tab = GetParameter<string>(parameters, "tab");

        if (string.IsNullOrEmpty(entityId)
            && (page.Equals(UiPageKeys.EditEmployee, StringComparison.OrdinalIgnoreCase)
                || page.Equals(UiPageKeys.EditAddress, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(SkillResult.Error(
                "Refusing to open the client editor without a client id — there is nothing to show yet. " +
                "To create a new client, call create_employee; only navigate here afterwards with the entityId of the created client."));
        }

        var entry = _pageKeyCatalog.GetByPageKey(page);
        if (entry == null)
        {
            return Task.FromResult(SkillResult.Error(
                $"Unknown page: {page}. Available pages: {string.Join(", ", _pageKeyCatalog.AllPageKeys)}"));
        }

        if (!string.IsNullOrEmpty(entry.RequiredPermission)
            && !Permissions.HasPermission(context.UserPermissions, entry.RequiredPermission))
        {
            return Task.FromResult(SkillResult.Error(
                $"User '{context.UserName}' is not allowed to open page '{page}' (requires permission '{entry.RequiredPermission}')."));
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
            QueryParams = queryParams
        };

        var explainSkill = PageExplainSkillRoutes.ResolveSkillName(route);
        var message = explainSkill == null
            ? $"Navigate to {page}"
            : $"Navigate to {page}. MANDATORY next step: if the user asked what this page is, how it works, " +
              $"or how to create/edit something here, call {explainSkill} with level=elements NOW and answer ONLY from its result.";

        return Task.FromResult(SkillResult.Navigation(navigationData, message));
    }
}
