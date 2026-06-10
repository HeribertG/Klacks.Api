// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fetches the full definition of a single shift (times, validity, weekdays, quantity,
/// client, groups) via GetQuery&lt;ShiftResource&gt;. Use search_shifts first to resolve
/// the shiftId.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to fetch.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_shift_details")]
public class GetShiftDetailsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetShiftDetailsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");

        ShiftResource? shift;
        try
        {
            shift = await _mediator.Send(new GetQuery<ShiftResource>(shiftId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        if (shift == null)
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        var details = new
        {
            shift.Id,
            shift.Name,
            shift.Abbreviation,
            shift.Description,
            shift.Status,
            shift.ShiftType,
            shift.FromDate,
            shift.UntilDate,
            shift.StartShift,
            shift.EndShift,
            shift.WorkTime,
            shift.Quantity,
            shift.SumEmployees,
            shift.IsSporadic,
            shift.IsTimeRange,
            shift.CuttingAfterMidnight,
            Weekdays = new
            {
                shift.IsMonday,
                shift.IsTuesday,
                shift.IsWednesday,
                shift.IsThursday,
                shift.IsFriday,
                shift.IsSaturday,
                shift.IsSunday,
                shift.IsHoliday
            },
            shift.ClientId,
            Client = shift.Client == null
                ? null
                : new { shift.Client.Company, shift.Client.FirstName, shift.Client.Name },
            Groups = shift.Groups.Select(g => new { g.Id, g.Name }).ToList()
        };

        return SkillResult.SuccessResult(details, $"Shift '{shift.Name}' loaded.");
    }
}
