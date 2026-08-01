// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a company-wide monthly target hours row: loads it via GetQuery, patches only the supplied
/// fields and persists via PutCommand. Fields that are not supplied keep their current value; a call
/// without changes is a successful no-op.
/// </summary>
/// <param name="monthlyTargetHoursId">UUID of the row to update (required).</param>
/// <param name="year">Optional new calendar year.</param>
/// <param name="month">Optional new calendar month 1-12.</param>
/// <param name="hours">Optional new target hours at full workload.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_monthly_target_hours")]
public class UpdateMonthlyTargetHoursSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateMonthlyTargetHoursSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var monthlyTargetHoursId = GetRequiredGuid(parameters, "monthlyTargetHoursId");

        MonthlyTargetHoursResource existing;
        try
        {
            existing = await _mediator.Send(
                new GetQuery<MonthlyTargetHoursResource>(monthlyTargetHoursId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Monthly target hours {monthlyTargetHoursId} not found.");
        }

        var changed = new List<string>();

        var year = GetParameter<int?>(parameters, "year");
        if (year.HasValue && year.Value != existing.Year)
        {
            existing.Year = year.Value;
            changed.Add("year");
        }

        var month = GetParameter<int?>(parameters, "month");
        if (month.HasValue && month.Value != existing.Month)
        {
            existing.Month = month.Value;
            changed.Add("month");
        }

        var hours = GetParameter<decimal?>(parameters, "hours");
        if (hours.HasValue && hours.Value != existing.Hours)
        {
            existing.Hours = hours.Value;
            changed.Add("hours");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { MonthlyTargetHoursId = monthlyTargetHoursId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — monthly target hours left unchanged.");
        }

        var resource = new MonthlyTargetHoursResource
        {
            Id = existing.Id,
            Year = existing.Year,
            Month = existing.Month,
            Hours = existing.Hours
        };

        MonthlyTargetHoursResource? updated;
        try
        {
            updated = await _mediator.Send(new PutCommand<MonthlyTargetHoursResource>(resource), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error(
                $"Update of monthly target hours {monthlyTargetHoursId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                MonthlyTargetHoursId = monthlyTargetHoursId,
                ChangedFields = changed,
                updated.Year,
                updated.Month,
                updated.Hours
            },
            $"Monthly target hours {updated.Year}-{updated.Month:00} updated ({string.Join(", ", changed)}), now {updated.Hours}.");
    }
}
