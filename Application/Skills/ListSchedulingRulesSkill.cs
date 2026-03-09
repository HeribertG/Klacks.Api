// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill to list all scheduling rules with optional filtering by search term.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ListSchedulingRulesSkill : BaseSkill
{
    private readonly ISchedulingRuleRepository _repository;

    public override string Name => "list_scheduling_rules";

    public override string Description =>
        "Lists all scheduling rules with their configuration values.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanViewSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "searchTerm",
            "Optional search term to filter scheduling rules by name",
            SkillParameterType.String,
            Required: false)
    ];

    public ListSchedulingRulesSkill(ISchedulingRuleRepository repository)
    {
        _repository = repository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");

        var allRules = await _repository.List();

        var rules = allRules
            .Where(r => !r.IsDeleted)
            .Where(r => string.IsNullOrEmpty(searchTerm) ||
                        r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.MaxWorkDays,
                r.MinRestDays,
                r.MinPauseHours,
                r.MaxOptimalGap,
                r.MaxDailyHours,
                r.MaxWeeklyHours,
                r.MaxConsecutiveDays,
                r.DefaultWorkingHours,
                r.OvertimeThreshold,
                r.GuaranteedHours,
                r.MaximumHours,
                r.MinimumHours,
                r.FullTimeHours,
                r.VacationDaysPerYear,
                r.NightRate,
                r.HolidayRate,
                r.SaRate,
                r.SoRate
            })
            .ToList();

        var resultData = new
        {
            Rules = rules,
            TotalCount = rules.Count
        };

        var message = $"Found {rules.Count} scheduling rule(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
