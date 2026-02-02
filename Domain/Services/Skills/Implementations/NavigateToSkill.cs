using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Domain.Services.Skills.Implementations;

public class NavigateToSkill : BaseSkill
{
    private static readonly Dictionary<string, string> Routes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "dashboard", "/dashboard" },
        { "employees", "/employees" },
        { "employee-details", "/employees" },
        { "schedule", "/schedule" },
        { "absences", "/absences" },
        { "reports", "/reports" },
        { "settings", "/settings" },
        { "groups", "/groups" },
        { "contracts", "/contracts" },
        { "holidays", "/holidays" }
    };

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
            EnumValues: Routes.Keys.ToList()),
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

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var page = GetRequiredString(parameters, "page");
        var entityId = GetParameter<string>(parameters, "entityId");
        var tab = GetParameter<string>(parameters, "tab");

        if (!Routes.TryGetValue(page, out var baseRoute))
        {
            return Task.FromResult(SkillResult.Error($"Unknown page: {page}. Available pages: {string.Join(", ", Routes.Keys)}"));
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

        return Task.FromResult(SkillResult.Navigation(navigationData, $"Navigate to {page}"));
    }
}
