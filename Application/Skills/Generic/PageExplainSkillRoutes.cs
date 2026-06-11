// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps frontend workplace routes to the explain_page_* skill that documents the page,
/// so the current page's explain skill can be guaranteed in the LLM tool list.
/// </summary>
/// <param name="currentRoute">Frontend route from AssistantPageContext; may carry entity ids, query strings or fragments</param>
namespace Klacks.Api.Application.Skills.Generic;

public static class PageExplainSkillRoutes
{
    private static readonly IReadOnlyDictionary<string, string> RouteToSkill =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/workplace/dashboard"] = "explain_page_dashboard",
            ["/workplace/schedule"] = "explain_page_schedule",
            ["/workplace/absence"] = "explain_page_absence",
            ["/workplace/client-availability"] = "explain_page_availability",
            ["/workplace/shift"] = "explain_page_shifts",
            ["/workplace/new-shift"] = "explain_page_shifts",
            ["/workplace/edit-shift"] = "explain_page_shifts",
            ["/workplace/cut-shift"] = "explain_page_shifts",
            ["/workplace/container-template"] = "explain_page_shifts",
            ["/workplace/client"] = "explain_page_employees",
            ["/workplace/edit-address"] = "explain_page_employees",
            ["/workplace/group"] = "explain_page_groups",
            ["/workplace/group-structure"] = "explain_page_groups",
            ["/workplace/edit-group"] = "explain_page_groups",
            ["/workplace/period-closing"] = "explain_page_period_closing",
            ["/workplace/inbox"] = "explain_page_inbox",
            ["/workplace/settings"] = "explain_page_settings_overview",
        };

    public static string? ResolveSkillName(string? currentRoute)
    {
        if (string.IsNullOrWhiteSpace(currentRoute))
        {
            return null;
        }

        var path = currentRoute.Split('?', '#')[0].TrimEnd('/');

        // Strip trailing path segments (entity ids etc.) until a known route matches.
        while (path.Length > 0)
        {
            if (RouteToSkill.TryGetValue(path, out var skillName))
            {
                return skillName;
            }

            var lastSlash = path.LastIndexOf('/');
            if (lastSlash <= 0)
            {
                return null;
            }

            path = path[..lastSlash];
        }

        return null;
    }
}
