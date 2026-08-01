// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a period cap rule: loads it via GetQuery, patches only the supplied fields and persists
/// via PutCommand. Moving the window away from CustomWeeks clears customPeriodWeeks, because the
/// handler rejects that field outside CustomWeeks and no parameter can express "unset".
/// </summary>
/// <param name="periodCapRuleId">UUID of the period cap rule to update (required).</param>
/// <param name="period">Optional new window: Month, Quarter, Year or CustomWeeks.</param>
/// <param name="scope">Optional new counted hours: TotalHours or OvertimeHours.</param>
/// <param name="capHours">Optional new hours for the window; 0 switches the rule out of fixed-period mode.</param>
/// <param name="warnAtPercent">Optional new warning percentage (1-100).</param>
/// <param name="customPeriodWeeks">Optional new length in weeks, only valid together with CustomWeeks.</param>
/// <param name="rollingWindowWeeks">Optional new rolling window length in weeks.</param>
/// <param name="maxAverageWeeklyHours">Optional new average weekly hours for the rolling window.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_period_cap_rule")]
public class UpdatePeriodCapRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdatePeriodCapRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var periodCapRuleId = GetRequiredGuid(parameters, "periodCapRuleId");

        PeriodCapRuleResource existing;
        try
        {
            existing = await _mediator.Send(new GetQuery<PeriodCapRuleResource>(periodCapRuleId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Period cap rule {periodCapRuleId} not found.");
        }

        var changed = new List<string>();

        var periodRaw = GetParameter<string>(parameters, "period");
        if (!string.IsNullOrWhiteSpace(periodRaw))
        {
            if (!Enum.TryParse<PeriodCapPeriod>(periodRaw, ignoreCase: true, out var period)
                || !Enum.IsDefined(period))
            {
                return SkillResult.Error(
                    $"Invalid period '{periodRaw}'. Use one of: {string.Join(", ", Enum.GetNames<PeriodCapPeriod>())}.");
            }

            if (period != existing.Period)
            {
                existing.Period = period;
                changed.Add("period");

                if (period != PeriodCapPeriod.CustomWeeks && existing.CustomPeriodWeeks.HasValue)
                {
                    existing.CustomPeriodWeeks = null;
                    changed.Add("customPeriodWeeks");
                }
            }
        }

        var scopeRaw = GetParameter<string>(parameters, "scope");
        if (!string.IsNullOrWhiteSpace(scopeRaw))
        {
            if (!Enum.TryParse<PeriodCapScope>(scopeRaw, ignoreCase: true, out var scope)
                || !Enum.IsDefined(scope))
            {
                return SkillResult.Error(
                    $"Invalid scope '{scopeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<PeriodCapScope>())}.");
            }

            if (scope != existing.Scope)
            {
                existing.Scope = scope;
                changed.Add("scope");
            }
        }

        var capHours = GetParameter<decimal?>(parameters, "capHours");
        if (capHours.HasValue && capHours.Value != existing.CapHours)
        {
            existing.CapHours = capHours.Value;
            changed.Add("capHours");
        }

        var warnAtPercent = GetParameter<int?>(parameters, "warnAtPercent");
        if (warnAtPercent.HasValue && warnAtPercent.Value != existing.WarnAtPercent)
        {
            existing.WarnAtPercent = warnAtPercent.Value;
            changed.Add("warnAtPercent");
        }

        var customPeriodWeeks = GetParameter<int?>(parameters, "customPeriodWeeks");
        if (customPeriodWeeks.HasValue && customPeriodWeeks.Value != existing.CustomPeriodWeeks)
        {
            existing.CustomPeriodWeeks = customPeriodWeeks.Value;
            if (!changed.Contains("customPeriodWeeks"))
            {
                changed.Add("customPeriodWeeks");
            }
        }

        var rollingWindowWeeks = GetParameter<int?>(parameters, "rollingWindowWeeks");
        if (rollingWindowWeeks.HasValue && rollingWindowWeeks.Value != existing.RollingWindowWeeks)
        {
            existing.RollingWindowWeeks = rollingWindowWeeks.Value;
            changed.Add("rollingWindowWeeks");
        }

        var maxAverageWeeklyHours = GetParameter<decimal?>(parameters, "maxAverageWeeklyHours");
        if (maxAverageWeeklyHours.HasValue && maxAverageWeeklyHours.Value != existing.MaxAverageWeeklyHours)
        {
            existing.MaxAverageWeeklyHours = maxAverageWeeklyHours.Value;
            changed.Add("maxAverageWeeklyHours");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { PeriodCapRuleId = periodCapRuleId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — period cap rule left unchanged.");
        }

        PeriodCapRuleResource? updated;
        try
        {
            updated = await _mediator.Send(new PutCommand<PeriodCapRuleResource>(existing), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error(
                $"Update of period cap rule {periodCapRuleId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                PeriodCapRuleId = periodCapRuleId,
                ChangedFields = changed,
                Period = updated.Period.ToString(),
                Scope = updated.Scope.ToString(),
                updated.CapHours,
                updated.WarnAtPercent,
                updated.CustomPeriodWeeks,
                updated.RollingWindowWeeks,
                updated.MaxAverageWeeklyHours
            },
            $"Period cap rule {periodCapRuleId} updated ({string.Join(", ", changed)}).");
    }
}
