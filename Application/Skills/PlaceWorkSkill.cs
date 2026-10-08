// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that assigns an agent (client) to a shift on a specific date — i.e. creates a single Work entry
/// in the main schedule or in a named scenario. Delegates to the existing BulkAddWorks pipeline so all
/// period-hour recalculation and validation logic stays in one place.
/// A client hidden from the caller by group visibility is answered exactly like an unknown client id.
/// A day that is sealed for the client is refused before anything is written, also for scenario placements (which
/// the day lock itself lets through) - a sealed day is a fixed cell for every planner.
/// </summary>
/// <param name="clientId">UUID of the client (agent) to schedule.</param>
/// <param name="shiftId">UUID of the shift to assign.</param>
/// <param name="date">Workday in ISO yyyy-MM-dd.</param>
/// <param name="startTime">Optional override start time HH:mm; defaults to the shift's startShift.</param>
/// <param name="endTime">Optional override end time HH:mm; defaults to the shift's endShift.</param>
/// <param name="workTime">Optional work time in hours; defaults to (endTime - startTime) wrapping past midnight.</param>
/// <param name="information">Free-text note.</param>
/// <param name="analyseToken">Optional scenario token; null = write to main schedule.</param>

using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
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
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ISealedDayRepository _sealedDayRepository;

    public PlaceWorkSkill(
        IMediator mediator,
        IShiftRepository shiftRepository,
        IPreCommitConflictChecker conflictChecker,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ISealedDayRepository sealedDayRepository)
    {
        _mediator = mediator;
        _shiftRepository = shiftRepository;
        _conflictChecker = conflictChecker;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _sealedDayRepository = sealedDayRepository;
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

        if (!await ClientResolver.ExistsVisibleAsync(_clientRepository, _clientVisibilityGuard, clientId, cancellationToken))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null)
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        if (await _sealedDayRepository.IsDayLockedAsync(date, clientId, cancellationToken))
        {
            return SkillResult.Error($"Day {date:yyyy-MM-dd} is sealed for client {clientId}; nothing was placed.");
        }

        var startTime = !string.IsNullOrWhiteSpace(startTimeRaw) && TimeOnly.TryParse(startTimeRaw, out var sParsed)
            ? sParsed
            : shift.StartShift;
        var endTime = !string.IsNullOrWhiteSpace(endTimeRaw) && TimeOnly.TryParse(endTimeRaw, out var eParsed)
            ? eParsed
            : shift.EndShift;
        var workTime = workTimeRaw ?? CalculateWorkTime(startTime, endTime);

        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }

        var plannedRow = new PlannedWorkRow(clientId, date, startTime, endTime, shiftId);
        var conflictCheck = await _conflictChecker.CheckAsync([plannedRow], analyseToken, cancellationToken);

        if (conflictCheck.HasBlocking)
        {
            var blocking = conflictCheck.NewConflicts
                .Where(c => c.Type == ScheduleValidationType.Error)
                .Select(c => new { c.Comment, c.Date, CommentParams = LocalizedCommentParams.ForLanguage(c.CommentParams, context.UserLanguage) })
                .ToList();
            return SkillResult.Error(
                $"Placement blocked: client {clientId} on shift '{shift.Name}' for {date} would introduce " +
                $"{blocking.Count} schedule conflict(s) (e.g. a collision). Not committed.",
                new Dictionary<string, object> { ["conflicts"] = blocking });
        }

        var warnings = conflictCheck.NewConflicts
            .Select(c => new { Severity = c.Type.ToString(), c.Comment, c.Date, CommentParams = LocalizedCommentParams.ForLanguage(c.CommentParams, context.UserLanguage) })
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
