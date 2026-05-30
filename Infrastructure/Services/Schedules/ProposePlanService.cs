// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IProposePlanService"/>. Mirrors WizardApplyService.ApplyAsScenarioAsync: create
/// an AnalyseScenario, clone the real schedule under its token, flush, THEN run the pre-commit guardrail
/// (so the checker sees the cloned world, not an empty one), then write the non-blocking placements via
/// the BulkAddWorks pipeline so period-hour/collision/notification side-effects fire. Blocking placements
/// (collisions) are skipped and reported, keeping the written scenario collision-free.
/// </summary>
/// <param name="shiftRepository">Resolves shift start/end times for each placement</param>
/// <param name="scenarioRepository">Persists the new AnalyseScenario</param>
/// <param name="scenarioService">Clones real schedule data under the scenario token</param>
/// <param name="conflictChecker">Pre-commit guardrail used against the cloned scenario world</param>
/// <param name="mediator">Dispatches BulkAddWorksCommand for the written placements</param>
/// <param name="unitOfWork">Flushes the scenario + clone before the guardrail check</param>
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ProposePlanService : IProposePlanService
{
    private const string ScenarioNamePrefix = "Proposal";
    private const string ShiftNotFoundReason = "shift not found";
    private const int HoursPerDay = 24;

    private readonly IShiftRepository _shiftRepository;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public ProposePlanService(
        IShiftRepository shiftRepository,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IPreCommitConflictChecker conflictChecker,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _conflictChecker = conflictChecker;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProposePlanOutcome> ProposeAsync(
        Guid? groupId,
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyList<PlacementInput> placements,
        CancellationToken cancellationToken = default)
    {
        var rejected = new List<RejectedPlacement>();

        var shiftIds = placements.Select(p => p.ShiftId).Distinct().ToList();
        var shifts = new Dictionary<Guid, Shift>();
        foreach (var shiftId in shiftIds)
        {
            var shift = await _shiftRepository.Get(shiftId);
            if (shift != null)
            {
                shifts[shiftId] = shift;
            }
        }

        var resolved = new List<PlacementInput>();
        foreach (var placement in placements)
        {
            if (shifts.ContainsKey(placement.ShiftId))
            {
                resolved.Add(placement);
            }
            else
            {
                rejected.Add(new RejectedPlacement(placement.ClientId, placement.ShiftId, placement.Date, ShiftNotFoundReason));
            }
        }

        var token = Guid.NewGuid();
        var name = await GenerateUniqueNameAsync(fromDate, untilDate, groupId, cancellationToken);
        var scenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = fromDate,
            UntilDate = untilDate,
            Token = token,
            RunGroupId = Guid.NewGuid()
        };
        await _scenarioRepository.Add(scenario);

        // Clone the real schedule under the token FIRST, then flush, so the guardrail sees the cloned
        // world. If the check ran before this, it would query by token, find nothing, and falsely pass.
        var shiftIdMap = await _scenarioService.CloneScenarioDataAsync(
            groupId, fromDate, untilDate, token, shiftIds, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var plannedRows = resolved
            .Select(p => (Placement: p, Row: new PlannedWorkRow(
                p.ClientId, p.Date, shifts[p.ShiftId].StartShift, shifts[p.ShiftId].EndShift, p.ShiftId)))
            .ToList();

        var (accepted, blocked, acceptedWarnings) = await PartitionAsync(plannedRows, token, cancellationToken);
        rejected.AddRange(blocked);

        var warnings = acceptedWarnings;
        if (accepted.Count > 0)
        {
            await WriteAsync(accepted, shifts, shiftIdMap, token, fromDate, untilDate, cancellationToken);
        }

        return new ProposePlanOutcome(
            scenario.Id,
            token,
            name,
            accepted.Select(a => a.Placement).ToList(),
            rejected,
            warnings);
    }

    private async Task<(
        List<(PlacementInput Placement, PlannedWorkRow Row)> Accepted,
        List<RejectedPlacement> Rejected,
        List<ScheduleValidationNotificationDto> Warnings)> PartitionAsync(
        List<(PlacementInput Placement, PlannedWorkRow Row)> plannedRows,
        Guid token,
        CancellationToken cancellationToken)
    {
        if (plannedRows.Count == 0)
        {
            return ([], [], []);
        }

        // Fast path: one batched check. CheckAsync builds all synthetic blocks per client together, so
        // proposed-vs-proposed collisions for the same client are caught. No blocking -> write everything
        // and reuse this same result for the warnings (no second check call).
        var batch = await _conflictChecker.CheckAsync(
            plannedRows.Select(p => p.Row).ToList(), token, cancellationToken);
        if (!batch.HasBlocking)
        {
            return (plannedRows, [], NonBlocking(batch));
        }

        // A blocking conflict exists somewhere: fall back to a greedy per-client accept so only the
        // placements actually involved in a collision are dropped (the rest still get proposed).
        var accepted = new List<(PlacementInput, PlannedWorkRow)>();
        var rejected = new List<RejectedPlacement>();
        var acceptedRowsByClient = new Dictionary<Guid, List<PlannedWorkRow>>();
        var lastCheckByClient = new Dictionary<Guid, PreCommitCheckResult>();

        foreach (var (placement, row) in plannedRows)
        {
            if (!acceptedRowsByClient.TryGetValue(placement.ClientId, out var clientRows))
            {
                clientRows = [];
                acceptedRowsByClient[placement.ClientId] = clientRows;
            }

            var trial = clientRows.Append(row).ToList();
            var check = await _conflictChecker.CheckAsync(trial, token, cancellationToken);
            if (check.HasBlocking)
            {
                var reason = check.NewConflicts.First(c => c.Type == ScheduleValidationType.Error).Comment;
                rejected.Add(new RejectedPlacement(placement.ClientId, placement.ShiftId, placement.Date, reason));
            }
            else
            {
                clientRows.Add(row);
                accepted.Add((placement, row));
                lastCheckByClient[placement.ClientId] = check;
            }
        }

        var warnings = lastCheckByClient.Values.SelectMany(NonBlocking).ToList();
        return (accepted, rejected, warnings);
    }

    private static List<ScheduleValidationNotificationDto> NonBlocking(PreCommitCheckResult result)
        => result.NewConflicts.Where(c => c.Type != ScheduleValidationType.Error).ToList();

    private async Task WriteAsync(
        List<(PlacementInput Placement, PlannedWorkRow Row)> accepted,
        IReadOnlyDictionary<Guid, Shift> shifts,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        Guid token,
        DateOnly fromDate,
        DateOnly untilDate,
        CancellationToken cancellationToken)
    {
        var bulkItems = accepted.Select(a =>
        {
            var shift = shifts[a.Placement.ShiftId];
            var cloneShiftId = shiftIdMap.TryGetValue(a.Placement.ShiftId, out var mapped)
                ? mapped
                : a.Placement.ShiftId;
            return new BulkWorkItem
            {
                ClientId = a.Placement.ClientId,
                ShiftId = cloneShiftId,
                CurrentDate = a.Placement.Date,
                StartTime = shift.StartShift,
                EndTime = shift.EndShift,
                WorkTime = WorkHours(shift.StartShift, shift.EndShift),
                AnalyseToken = token
            };
        }).ToList();

        await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
        {
            Works = bulkItems,
            PeriodStart = fromDate,
            PeriodEnd = untilDate
        }), cancellationToken);
    }

    private async Task<string> GenerateUniqueNameAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        var baseName = $"{ScenarioNamePrefix} {from:dd.MM.yy} – {until:dd.MM.yy}";
        var existing = await _scenarioRepository.GetByGroupAsync(groupId, cancellationToken);
        var existingNames = existing.Select(s => s.Name).ToHashSet();

        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }

        var counter = 2;
        while (existingNames.Contains($"{baseName} ({counter})"))
        {
            counter++;
        }
        return $"{baseName} ({counter})";
    }

    private static decimal WorkHours(TimeOnly start, TimeOnly end)
    {
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalHours <= 0)
        {
            duration = duration.Add(TimeSpan.FromHours(HoursPerDay));
        }
        return (decimal)duration.TotalHours;
    }
}
