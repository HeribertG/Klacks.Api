// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the built-in scheduling default fallbacks (min rest hours, max daily hours, max
/// consecutive days, max weekly hours, min rest days per week). These are the values the validator
/// falls back to when neither settings nor a contract nor a scheduling rule carries a number;
/// per-client effective limits are resolved settings -> contract -> scheduling rule. Klacksy uses
/// this to state the hard limits a rule-compliant plan must respect. Read-only; takes no parameters.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_scheduling_defaults")]
public class GetSchedulingDefaultsSkill : BaseSkillImplementation
{
    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var data = new
        {
            MinRestHours = SchedulingPolicyDefaults.MinRestHours,
            MaxDailyHours = SchedulingPolicyDefaults.MaxDailyHours,
            MaxConsecutiveDays = SchedulingPolicyDefaults.MaxConsecutiveDays,
            MaxWeeklyHours = SchedulingPolicyDefaults.MaxWeeklyHours,
            MinRestDays = SchedulingPolicyDefaults.MinRestDays,
            IsFallback = true
        };

        var message =
            $"Built-in scheduling default fallbacks: min rest {SchedulingPolicyDefaults.MinRestHours:0}h between blocks, " +
            $"max {SchedulingPolicyDefaults.MaxDailyHours:0}h/day, max {SchedulingPolicyDefaults.MaxWeeklyHours:0}h/week, " +
            $"max {SchedulingPolicyDefaults.MaxConsecutiveDays} consecutive days, min {SchedulingPolicyDefaults.MinRestDays} rest days/week. " +
            "Per-client effective limits are resolved settings -> contract -> scheduling rule.";

        return Task.FromResult(SkillResult.SuccessResult(data, message));
    }
}
