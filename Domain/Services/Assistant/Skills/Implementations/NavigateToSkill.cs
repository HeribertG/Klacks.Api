// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that navigates to a known page in the Klacks application by page key (e.g. dashboard, settings, schedule).
/// Use this for known, fixed pages. For searching a person or entity by name and opening their detail page,
/// use search_and_navigate instead.
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("navigate_to")]
public class NavigateToSkill : BaseSkillImplementation
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;

    public NavigateToSkill(
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository)
    {
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var page = GetRequiredString(parameters, "page");
        var entityId = GetParameter<string>(parameters, "entityId");
        var tab = GetParameter<string>(parameters, "tab");

        var routes = await LoadRoutesFromDbAsync(cancellationToken);

        if (!routes.TryGetValue(page, out var baseRoute))
        {
            return SkillResult.Error($"Unknown page: {page}. Available pages: {string.Join(", ", routes.Keys)}");
        }

        var route = baseRoute;
        if (!string.IsNullOrEmpty(entityId))
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

        return SkillResult.Navigation(navigationData, $"Navigate to {page}");
    }

    private async Task<Dictionary<string, string>> LoadRoutesFromDbAsync(CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var skill = await _agentSkillRepository.GetByNameAsync(agent.Id, "navigate_to", cancellationToken);
        if (skill == null || string.IsNullOrEmpty(skill.HandlerConfig) || skill.HandlerConfig == "{}")
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var doc = JsonDocument.Parse(skill.HandlerConfig);
            if (doc.RootElement.TryGetProperty("routes", out var routesElement))
            {
                var routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in routesElement.EnumerateObject())
                {
                    routes[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
                return routes;
            }
        }
        catch (JsonException)
        {
            // Fallback on malformed config
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
