// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a restricted time window rule via PostCommand. Season bounds and the daily window are
/// checked by the handler; its message is relayed so the caller learns which bound was rejected.
/// </summary>
/// <param name="seasonFromMonth">Month the season starts in (1-12).</param>
/// <param name="seasonFromDay">Day of month the season starts on (1-31).</param>
/// <param name="seasonToMonth">Month the season ends in (1-12).</param>
/// <param name="seasonToDay">Day of month the season ends on (1-31).</param>
/// <param name="dailyStart">Start of the daily window as HH:mm.</param>
/// <param name="dailyEnd">End of the daily window as HH:mm; a value not greater than dailyStart wraps past midnight, and a value equal to dailyStart means a full 24 hour ban window.</param>
/// <param name="appliesToGroupTag">Group tag the rule applies to.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_restricted_time_window_rule")]
public class CreateRestrictedTimeWindowRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateRestrictedTimeWindowRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dailyStart = GetParameter<TimeOnly?>(parameters, "dailyStart");
        if (!dailyStart.HasValue)
        {
            return SkillResult.Error("Parameter 'dailyStart' is required and must look like 22:00.");
        }

        var dailyEnd = GetParameter<TimeOnly?>(parameters, "dailyEnd");
        if (!dailyEnd.HasValue)
        {
            return SkillResult.Error("Parameter 'dailyEnd' is required and must look like 06:00.");
        }

        var resource = new RestrictedTimeWindowRuleResource
        {
            SeasonFromMonth = GetRequiredInt(parameters, "seasonFromMonth"),
            SeasonFromDay = GetRequiredInt(parameters, "seasonFromDay"),
            SeasonToMonth = GetRequiredInt(parameters, "seasonToMonth"),
            SeasonToDay = GetRequiredInt(parameters, "seasonToDay"),
            DailyStart = dailyStart.Value,
            DailyEnd = dailyEnd.Value,
            AppliesToGroupTag = GetParameter<string>(parameters, "appliesToGroupTag")?.Trim() ?? string.Empty
        };

        RestrictedTimeWindowRuleResource? created;
        try
        {
            created = await _mediator.Send(
                new PostCommand<RestrictedTimeWindowRuleResource>(resource), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (created == null)
        {
            return SkillResult.Error("Creating the restricted time window rule returned no result.");
        }

        return SkillResult.SuccessResult(
            new
            {
                created.Id,
                Season = $"{created.SeasonFromMonth:00}-{created.SeasonFromDay:00} to {created.SeasonToMonth:00}-{created.SeasonToDay:00}",
                DailyStart = created.DailyStart.ToString("HH:mm"),
                DailyEnd = created.DailyEnd.ToString("HH:mm"),
                created.AppliesToGroupTag
            },
            $"Restricted time window rule created: {created.DailyStart:HH\\:mm}-{created.DailyEnd:HH\\:mm} " +
            $"between {created.SeasonFromMonth:00}-{created.SeasonFromDay:00} and {created.SeasonToMonth:00}-{created.SeasonToDay:00} (id {created.Id}).");
    }
}
