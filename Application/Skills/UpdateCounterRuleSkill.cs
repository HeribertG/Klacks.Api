// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a counter rule: loads it via GetQuery, patches only the supplied fields and persists via
/// PutCommand. Fields that are not supplied keep their current value; a call without changes is a
/// successful no-op.
/// </summary>
/// <param name="counterRuleId">UUID of the counter rule to update (required).</param>
/// <param name="eventType">Optional new counted event: NightShift, WorkedDayInWeek or ShiftExceedingHours.</param>
/// <param name="period">Optional new window: Week, Month or Year.</param>
/// <param name="threshold">Optional new number of acceptable occurrences.</param>
/// <param name="hoursThreshold">Optional new duration in hours a shift must exceed to be counted.</param>
/// <param name="enforcement">Optional new reaction: Warn or Block.</param>

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

[SkillImplementation("update_counter_rule")]
public class UpdateCounterRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateCounterRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var counterRuleId = GetRequiredGuid(parameters, "counterRuleId");

        CounterRuleResource existing;
        try
        {
            existing = await _mediator.Send(new GetQuery<CounterRuleResource>(counterRuleId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Counter rule {counterRuleId} not found.");
        }

        var changed = new List<string>();

        var eventTypeRaw = GetParameter<string>(parameters, "eventType");
        if (!string.IsNullOrWhiteSpace(eventTypeRaw))
        {
            if (!Enum.TryParse<CounterEventType>(eventTypeRaw, ignoreCase: true, out var eventType)
                || !Enum.IsDefined(eventType))
            {
                return SkillResult.Error(
                    $"Invalid eventType '{eventTypeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<CounterEventType>())}.");
            }

            if (eventType != existing.EventType)
            {
                existing.EventType = eventType;
                changed.Add("eventType");
            }
        }

        var periodRaw = GetParameter<string>(parameters, "period");
        if (!string.IsNullOrWhiteSpace(periodRaw))
        {
            if (!Enum.TryParse<CounterPeriod>(periodRaw, ignoreCase: true, out var period)
                || !Enum.IsDefined(period))
            {
                return SkillResult.Error(
                    $"Invalid period '{periodRaw}'. Use one of: {string.Join(", ", Enum.GetNames<CounterPeriod>())}.");
            }

            if (period != existing.Period)
            {
                existing.Period = period;
                changed.Add("period");
            }
        }

        var threshold = GetParameter<int?>(parameters, "threshold");
        if (threshold.HasValue && threshold.Value != existing.Threshold)
        {
            existing.Threshold = threshold.Value;
            changed.Add("threshold");
        }

        var hoursThreshold = GetParameter<decimal?>(parameters, "hoursThreshold");
        if (hoursThreshold.HasValue && hoursThreshold.Value != existing.HoursThreshold)
        {
            existing.HoursThreshold = hoursThreshold.Value;
            changed.Add("hoursThreshold");
        }

        var enforcementRaw = GetParameter<string>(parameters, "enforcement");
        if (!string.IsNullOrWhiteSpace(enforcementRaw))
        {
            if (!Enum.TryParse<RuleEnforcementMode>(enforcementRaw, ignoreCase: true, out var enforcement)
                || !Enum.IsDefined(enforcement))
            {
                return SkillResult.Error(
                    $"Invalid enforcement '{enforcementRaw}'. Use one of: {string.Join(", ", Enum.GetNames<RuleEnforcementMode>())}.");
            }

            if (enforcement != existing.Enforcement)
            {
                existing.Enforcement = enforcement;
                changed.Add("enforcement");
            }
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { CounterRuleId = counterRuleId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — counter rule left unchanged.");
        }

        CounterRuleResource? updated;
        try
        {
            updated = await _mediator.Send(new PutCommand<CounterRuleResource>(existing), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error(
                $"Update of counter rule {counterRuleId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                CounterRuleId = counterRuleId,
                ChangedFields = changed,
                EventType = updated.EventType.ToString(),
                Period = updated.Period.ToString(),
                updated.Threshold,
                updated.HoursThreshold,
                Enforcement = updated.Enforcement?.ToString()
            },
            $"Counter rule {counterRuleId} updated ({string.Join(", ", changed)}).");
    }
}
