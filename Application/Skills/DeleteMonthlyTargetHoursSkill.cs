// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a company-wide monthly target hours row via DeleteCommand. Removing a row drops the
/// override for that month, so the resolution falls back to the contract again.
/// </summary>
/// <param name="monthlyTargetHoursId">UUID of the row to delete (required).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_monthly_target_hours")]
public class DeleteMonthlyTargetHoursSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteMonthlyTargetHoursSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var monthlyTargetHoursId = GetRequiredGuid(parameters, "monthlyTargetHoursId");

        var deleted = await _mediator.Send(
            new DeleteCommand<MonthlyTargetHoursResource>(monthlyTargetHoursId), cancellationToken);

        if (deleted == null)
        {
            return SkillResult.Error($"Monthly target hours {monthlyTargetHoursId} not found.");
        }

        return SkillResult.SuccessResult(
            new { MonthlyTargetHoursId = monthlyTargetHoursId, deleted.Year, deleted.Month, deleted.Hours },
            $"Monthly target hours for {deleted.Year}-{deleted.Month:00} removed.");
    }
}
