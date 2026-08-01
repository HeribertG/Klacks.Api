// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a period cap rule via PostCommand. The skill parses the enum inputs and leaves the mode
/// rules (fixed-period versus rolling-average, custom week bounds) to the handler, relaying its
/// message so the caller learns which combination was rejected and why.
/// </summary>
/// <param name="period">Window the cap applies to: Month, Quarter, Year or CustomWeeks.</param>
/// <param name="scope">Which hours are counted: TotalHours or OvertimeHours.</param>
/// <param name="capHours">Hours the window may reach in fixed-period mode.</param>
/// <param name="warnAtPercent">Optional percentage of the cap at which a warning is raised (1-100).</param>
/// <param name="customPeriodWeeks">Length in weeks, required when period is CustomWeeks.</param>
/// <param name="rollingWindowWeeks">Window length in weeks for rolling-average mode.</param>
/// <param name="maxAverageWeeklyHours">Average weekly hours the rolling window may reach.</param>
/// <param name="schedulingRuleId">Optional scheduling rule set this rule belongs to.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_period_cap_rule")]
public class CreatePeriodCapRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreatePeriodCapRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var periodRaw = GetParameter<string>(parameters, "period");
        if (!Enum.TryParse<PeriodCapPeriod>(periodRaw, ignoreCase: true, out var period)
            || !Enum.IsDefined(period))
        {
            return SkillResult.Error(
                $"Invalid period '{periodRaw}'. Use one of: {string.Join(", ", Enum.GetNames<PeriodCapPeriod>())}.");
        }

        var scopeRaw = GetParameter<string>(parameters, "scope");
        if (!Enum.TryParse<PeriodCapScope>(scopeRaw, ignoreCase: true, out var scope)
            || !Enum.IsDefined(scope))
        {
            return SkillResult.Error(
                $"Invalid scope '{scopeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<PeriodCapScope>())}.");
        }

        var resource = new PeriodCapRuleResource
        {
            Period = period,
            Scope = scope,
            CapHours = GetParameter<decimal?>(parameters, "capHours") ?? 0m,
            WarnAtPercent = GetParameter<int?>(parameters, "warnAtPercent"),
            CustomPeriodWeeks = GetParameter<int?>(parameters, "customPeriodWeeks"),
            RollingWindowWeeks = GetParameter<int?>(parameters, "rollingWindowWeeks"),
            MaxAverageWeeklyHours = GetParameter<decimal?>(parameters, "maxAverageWeeklyHours"),
            SchedulingRuleId = GetParameter<Guid?>(parameters, "schedulingRuleId")
        };

        PeriodCapRuleResource? created;
        try
        {
            created = await _mediator.Send(new PostCommand<PeriodCapRuleResource>(resource), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (created == null)
        {
            return SkillResult.Error("Creating the period cap rule returned no result.");
        }

        var summary = created.RollingWindowWeeks.HasValue
            ? $"{created.MaxAverageWeeklyHours} average weekly hours over {created.RollingWindowWeeks} weeks"
            : $"{created.CapHours} hours per {created.Period}";

        return SkillResult.SuccessResult(
            new
            {
                created.Id,
                Period = created.Period.ToString(),
                Scope = created.Scope.ToString(),
                created.CapHours,
                created.WarnAtPercent,
                created.CustomPeriodWeeks,
                created.RollingWindowWeeks,
                created.MaxAverageWeeklyHours
            },
            $"Period cap rule created for {created.Scope}: {summary} (id {created.Id}).");
    }
}
