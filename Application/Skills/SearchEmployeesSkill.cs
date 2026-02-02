using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class SearchEmployeesSkill : BaseSkill
{
    private readonly IClientSearchRepository _searchRepository;

    public override string Name => "search_employees";

    public override string Description =>
        "Search for employees or customers by various criteria like name, canton, or membership type. " +
        "Returns a list of matching employees with their basic information.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewClients" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "searchTerm",
            "Search term for name, company, or other text fields",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "canton",
            "Filter by Swiss canton code",
            SkillParameterType.Enum,
            Required: false,
            EnumValues: new List<string> { "BE", "ZH", "SG", "VD", "AG", "LU", "BS", "BL", "GR", "TG", "GE", "NE" }),
        new SkillParameter(
            "entityType",
            "Filter by entity type (0=Employee, 1=ExternEmp, 2=Customer)",
            SkillParameterType.Integer,
            Required: false),
        new SkillParameter(
            "limit",
            "Maximum number of results to return",
            SkillParameterType.Integer,
            Required: false,
            DefaultValue: 10)
    };

    public SearchEmployeesSkill(IClientSearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var canton = GetParameter<string>(parameters, "canton");
        var entityTypeValue = GetParameter<int?>(parameters, "entityType");
        var limit = GetParameter<int?>(parameters, "limit") ?? 10;

        EntityTypeEnum? entityType = entityTypeValue.HasValue
            ? (EntityTypeEnum)entityTypeValue.Value
            : null;

        var result = await _searchRepository.SearchAsync(
            searchTerm,
            canton,
            entityType,
            limit,
            cancellationToken);

        var resultData = new
        {
            Results = result.Items,
            Count = result.Items.Count,
            TotalCount = result.TotalCount,
            HasMore = result.TotalCount > limit
        };

        var message = result.TotalCount == 0
            ? "No employees found matching the criteria."
            : $"Found {result.TotalCount} employee(s)" +
              (!string.IsNullOrWhiteSpace(searchTerm) ? $" matching '{searchTerm}'" : "") +
              (!string.IsNullOrWhiteSpace(canton) ? $" in canton {canton}" : "") +
              (result.TotalCount > limit ? $". Showing first {limit}." : ".");

        return SkillResult.SuccessResult(resultData, message);
    }
}
