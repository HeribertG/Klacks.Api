// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a restricted time window rule: loads it via GetQuery, patches only the supplied fields
/// and persists via PutCommand. Fields that are not supplied keep their current value; a call
/// without changes is a successful no-op.
/// </summary>
/// <param name="restrictedTimeWindowRuleId">UUID of the rule to update (required).</param>
/// <param name="seasonFromMonth">Optional new start month (1-12).</param>
/// <param name="seasonFromDay">Optional new start day (1-31).</param>
/// <param name="seasonToMonth">Optional new end month (1-12).</param>
/// <param name="seasonToDay">Optional new end day (1-31).</param>
/// <param name="dailyStart">Optional new start of the daily window as HH:mm.</param>
/// <param name="dailyEnd">Optional new end of the daily window as HH:mm.</param>
/// <param name="appliesToGroupTag">Optional new group tag.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_restricted_time_window_rule")]
public class UpdateRestrictedTimeWindowRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateRestrictedTimeWindowRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var restrictedTimeWindowRuleId = GetRequiredGuid(parameters, "restrictedTimeWindowRuleId");

        RestrictedTimeWindowRuleResource existing;
        try
        {
            existing = await _mediator.Send(
                new GetQuery<RestrictedTimeWindowRuleResource>(restrictedTimeWindowRuleId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Restricted time window rule {restrictedTimeWindowRuleId} not found.");
        }

        var changed = new List<string>();

        var seasonFromMonth = GetParameter<int?>(parameters, "seasonFromMonth");
        if (seasonFromMonth.HasValue && seasonFromMonth.Value != existing.SeasonFromMonth)
        {
            existing.SeasonFromMonth = seasonFromMonth.Value;
            changed.Add("seasonFromMonth");
        }

        var seasonFromDay = GetParameter<int?>(parameters, "seasonFromDay");
        if (seasonFromDay.HasValue && seasonFromDay.Value != existing.SeasonFromDay)
        {
            existing.SeasonFromDay = seasonFromDay.Value;
            changed.Add("seasonFromDay");
        }

        var seasonToMonth = GetParameter<int?>(parameters, "seasonToMonth");
        if (seasonToMonth.HasValue && seasonToMonth.Value != existing.SeasonToMonth)
        {
            existing.SeasonToMonth = seasonToMonth.Value;
            changed.Add("seasonToMonth");
        }

        var seasonToDay = GetParameter<int?>(parameters, "seasonToDay");
        if (seasonToDay.HasValue && seasonToDay.Value != existing.SeasonToDay)
        {
            existing.SeasonToDay = seasonToDay.Value;
            changed.Add("seasonToDay");
        }

        var dailyStart = GetParameter<TimeOnly?>(parameters, "dailyStart");
        if (dailyStart.HasValue && dailyStart.Value != existing.DailyStart)
        {
            existing.DailyStart = dailyStart.Value;
            changed.Add("dailyStart");
        }

        var dailyEnd = GetParameter<TimeOnly?>(parameters, "dailyEnd");
        if (dailyEnd.HasValue && dailyEnd.Value != existing.DailyEnd)
        {
            existing.DailyEnd = dailyEnd.Value;
            changed.Add("dailyEnd");
        }

        var appliesToGroupTag = GetParameter<string>(parameters, "appliesToGroupTag");
        if (appliesToGroupTag != null && appliesToGroupTag.Trim() != existing.AppliesToGroupTag)
        {
            existing.AppliesToGroupTag = appliesToGroupTag.Trim();
            changed.Add("appliesToGroupTag");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { RestrictedTimeWindowRuleId = restrictedTimeWindowRuleId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — restricted time window rule left unchanged.");
        }

        RestrictedTimeWindowRuleResource? updated;
        try
        {
            updated = await _mediator.Send(
                new PutCommand<RestrictedTimeWindowRuleResource>(existing), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error(
                $"Update of restricted time window rule {restrictedTimeWindowRuleId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                RestrictedTimeWindowRuleId = restrictedTimeWindowRuleId,
                ChangedFields = changed,
                Season = $"{updated.SeasonFromMonth:00}-{updated.SeasonFromDay:00} to {updated.SeasonToMonth:00}-{updated.SeasonToDay:00}",
                DailyStart = updated.DailyStart.ToString("HH:mm"),
                DailyEnd = updated.DailyEnd.ToString("HH:mm"),
                updated.AppliesToGroupTag
            },
            $"Restricted time window rule {restrictedTimeWindowRuleId} updated ({string.Join(", ", changed)}).");
    }
}
