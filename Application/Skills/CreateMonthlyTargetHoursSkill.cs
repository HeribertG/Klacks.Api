// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a company-wide monthly target hours row via PostCommand. The handler rejects a month that
/// already carries a row, so the skill relays that refusal instead of silently creating a duplicate.
/// </summary>
/// <param name="year">Calendar year the row applies to (required).</param>
/// <param name="month">Calendar month 1-12 the row applies to (required).</param>
/// <param name="hours">Target hours at full workload, for example 184 (required).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_monthly_target_hours")]
public class CreateMonthlyTargetHoursSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateMonthlyTargetHoursSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var year = GetRequiredInt(parameters, "year");
        var month = GetRequiredInt(parameters, "month");
        var hours = GetParameter<decimal?>(parameters, "hours");

        if (!hours.HasValue)
        {
            return SkillResult.Error("Parameter 'hours' is required.");
        }

        var resource = new MonthlyTargetHoursResource
        {
            Year = year,
            Month = month,
            Hours = hours.Value
        };

        MonthlyTargetHoursResource? created;
        try
        {
            created = await _mediator.Send(new PostCommand<MonthlyTargetHoursResource>(resource), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (created == null)
        {
            return SkillResult.Error($"Creating monthly target hours for {year}-{month:00} returned no result.");
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.Year, created.Month, created.Hours },
            $"Monthly target hours for {created.Year}-{created.Month:00} set to {created.Hours} (id {created.Id}).");
    }
}
