// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that assigns an agent (client) to a shift on a specific date — i.e. creates a single Work entry
/// in the main schedule or in a named scenario. Delegates to the existing BulkAddWorks pipeline so all
/// period-hour recalculation and validation logic stays in one place.
/// </summary>
/// <param name="clientId">UUID of the client (agent) to schedule.</param>
/// <param name="shiftId">UUID of the shift to assign.</param>
/// <param name="date">Workday in ISO yyyy-MM-dd.</param>
/// <param name="startTime">Optional override start time HH:mm; defaults to the shift's startShift.</param>
/// <param name="endTime">Optional override end time HH:mm; defaults to the shift's endShift.</param>
/// <param name="workTime">Optional work time in hours; defaults to (endTime - startTime) wrapping past midnight.</param>
/// <param name="information">Free-text note.</param>
/// <param name="analyseToken">Optional scenario token; null = write to main schedule.</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("place_work")]
public class PlaceWorkSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IShiftRepository _shiftRepository;
    private readonly IPreCommitConflictChecker _conflictChecker;

    public PlaceWorkSkill(
        IMediator mediator,
        IShiftRepository shiftRepository,
        IPreCommitConflictChecker conflictChecker)
    {
        _mediator = mediator;
        _shiftRepository = shiftRepository;
        _conflictChecker = conflictChecker;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var startTimeRaw = GetParameter<string>(parameters, "startTime");
        var endTimeRaw = GetParameter<string>(parameters, "endTime");
        var workTimeRaw = GetParameter<decimal?>(parameters, "workTime");
        var information = GetParameter<string>(parameters, "information");
        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null)
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        var startTime = !string.IsNullOrWhiteSpace(startTimeRaw) && TimeOnly.TryParse(startTimeRaw, out var sParsed)
            ? sParsed
            : shift.StartShift;
        var endTime = !string.IsNullOrWhiteSpace(endTimeRaw) && TimeOnly.TryParse(endTimeRaw, out var eParsed)
            ? eParsed
            : shift.EndShift;
        var workTime = workTimeRaw ?? CalculateWorkTime(startTime, endTime);

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenRaw) && Guid.TryParse(analyseTokenRaw, out var atParsed))
        {
            analyseToken = atParsed;
        }

        var plannedRow = new PlannedWorkRow(clientId, date, startTime, endTime, shiftId);
        var conflictCheck = await _conflictChecker.CheckAsync([plannedRow], analyseToken, cancellationToken);

        if (conflictCheck.HasBlocking)
        {
            var blocking = conflictCheck.NewConflicts
                .Where(c => c.Type == ScheduleValidationType.Error)
                .Select(c => new { c.Comment, c.Date, c.CommentParams })
                .ToList();
            return SkillResult.Error(
                $"Placement blocked: client {clientId} on shift '{shift.Name}' for {date} would introduce " +
                $"{blocking.Count} schedule conflict(s) (e.g. a collision). Not committed.",
                new Dictionary<string, object> { ["conflicts"] = blocking });
        }

        var warnings = conflictCheck.NewConflicts
            .Select(c => new { Severity = c.Type.ToString(), c.Comment, c.Date, c.CommentParams })
            .ToList();

        var request = new BulkAddWorksRequest
        {
            PeriodStart = date,
            PeriodEnd = date,
            Works =
            [
                new BulkWorkItem
                {
                    ClientId = clientId,
                    ShiftId = shiftId,
                    CurrentDate = date,
                    StartTime = startTime,
                    EndTime = endTime,
                    WorkTime = workTime,
                    Information = information,
                    AnalyseToken = analyseToken
                }
            ]
        };

        var response = await _mediator.Send(new BulkAddWorksCommand(request), cancellationToken);

        var warningNote = warnings.Count > 0
            ? $" Note: {warnings.Count} schedule warning(s) (e.g. rest time / overtime) — committed anyway."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                ShiftId = shiftId,
                ShiftName = shift.Name,
                ClientId = clientId,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                WorkTime = workTime,
                AnalyseToken = analyseToken,
                Warnings = warnings,
                BulkResponse = response
            },
            $"Placed client {clientId} on shift '{shift.Name}' for {date} ({startTime}–{endTime}, {workTime}h).{warningNote}");
    }
}
