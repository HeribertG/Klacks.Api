// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

public class NavigateToSkill : BaseSkill
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;

    private static readonly Dictionary<string, string> FallbackRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "dashboard", "/workplace/dashboard" },
        { "employees", "/workplace/client" },
        { "employee-details", "/workplace/client" },
        { "schedule", "/workplace/schedule" },
        { "absences", "/workplace/absence" },
        { "reports", "/workplace/dashboard" },
        { "settings", "/workplace/settings" },
        { "groups", "/workplace/group" },
        { "contracts", "/workplace/client" },
        { "holidays", "/workplace/settings" }
    };

    public NavigateToSkill(
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository)
    {
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
    }

    public override string Name => "navigate_to";
    public override string Description => "Navigate to a specific page in the Klacks application";
    public override SkillCategory Category => SkillCategory.UI;

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "page",
            "The page to navigate to",
            SkillParameterType.Enum,
            Required: true,
            EnumValues: FallbackRoutes.Keys.ToList()),
        new SkillParameter(
            "entityId",
            "The ID of the entity to view (for detail pages)",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "tab",
            "The specific tab to open on the page",
            SkillParameterType.String,
            Required: false)
    };

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
            return FallbackRoutes;

        var skill = await _agentSkillRepository.GetByNameAsync(agent.Id, Name, cancellationToken);
        if (skill == null || string.IsNullOrEmpty(skill.HandlerConfig) || skill.HandlerConfig == "{}")
            return FallbackRoutes;

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

        return FallbackRoutes;
    }
}
