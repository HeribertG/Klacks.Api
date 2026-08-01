// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a counter rule via PostCommand. Enum inputs are parsed case-insensitively and rejected
/// with the list of accepted values, so an unknown word never silently becomes the default member.
/// </summary>
/// <param name="eventType">What is counted: NightShift, WorkedDayInWeek or ShiftExceedingHours.</param>
/// <param name="period">Window the count resets in: Week, Month or Year.</param>
/// <param name="threshold">How many occurrences are still acceptable.</param>
/// <param name="hoursThreshold">Duration in hours a shift must exceed to be counted, for ShiftExceedingHours.</param>
/// <param name="enforcement">Warn or Block once the count is exceeded.</param>
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

[SkillImplementation("create_counter_rule")]
public class CreateCounterRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateCounterRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var eventTypeRaw = GetParameter<string>(parameters, "eventType");
        if (!Enum.TryParse<CounterEventType>(eventTypeRaw, ignoreCase: true, out var eventType)
            || !Enum.IsDefined(eventType))
        {
            return SkillResult.Error(
                $"Invalid eventType '{eventTypeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<CounterEventType>())}.");
        }

        var periodRaw = GetParameter<string>(parameters, "period");
        if (!Enum.TryParse<CounterPeriod>(periodRaw, ignoreCase: true, out var period)
            || !Enum.IsDefined(period))
        {
            return SkillResult.Error(
                $"Invalid period '{periodRaw}'. Use one of: {string.Join(", ", Enum.GetNames<CounterPeriod>())}.");
        }

        var threshold = GetParameter<int?>(parameters, "threshold");
        if (!threshold.HasValue)
        {
            return SkillResult.Error("Parameter 'threshold' is required.");
        }

        RuleEnforcementMode? enforcement = null;
        var enforcementRaw = GetParameter<string>(parameters, "enforcement");
        if (!string.IsNullOrWhiteSpace(enforcementRaw))
        {
            if (!Enum.TryParse<RuleEnforcementMode>(enforcementRaw, ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                return SkillResult.Error(
                    $"Invalid enforcement '{enforcementRaw}'. Use one of: {string.Join(", ", Enum.GetNames<RuleEnforcementMode>())}.");
            }

            enforcement = parsed;
        }

        var resource = new CounterRuleResource
        {
            EventType = eventType,
            Period = period,
            Threshold = threshold.Value,
            HoursThreshold = GetParameter<decimal?>(parameters, "hoursThreshold"),
            Enforcement = enforcement,
            SchedulingRuleId = GetParameter<Guid?>(parameters, "schedulingRuleId")
        };

        CounterRuleResource? created;
        try
        {
            created = await _mediator.Send(new PostCommand<CounterRuleResource>(resource), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (created == null)
        {
            return SkillResult.Error("Creating the counter rule returned no result.");
        }

        return SkillResult.SuccessResult(
            new
            {
                created.Id,
                EventType = created.EventType.ToString(),
                Period = created.Period.ToString(),
                created.Threshold,
                created.HoursThreshold,
                Enforcement = created.Enforcement?.ToString()
            },
            $"Counter rule for {created.EventType} per {created.Period} created with threshold {created.Threshold} (id {created.Id}).");
    }
}
