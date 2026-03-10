// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that searches for employees or customers by criteria such as name, canton, or entity type
/// and returns a filtered list of matches with basic information.
/// To search AND directly navigate to a specific person, use search_and_navigate instead.
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("search_employees")]
public class SearchEmployeesSkill : BaseSkillImplementation
{
    private readonly IClientSearchRepository _searchRepository;

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
        var entityTypeValue = GetParameter<string>(parameters, "entityType");
        var limit = GetParameter<int?>(parameters, "limit") ?? 10;

        EntityTypeEnum? entityType = entityTypeValue switch
        {
            "Employee" => EntityTypeEnum.Employee,
            "ExternEmp" => EntityTypeEnum.ExternEmp,
            "Customer" => EntityTypeEnum.Customer,
            _ => null
        };

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
