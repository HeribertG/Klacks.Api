// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="CoverAbsenceCommand"/>. Orchestrates the disruption flow in one call: create a
/// scenario and clone the real schedule under its token, build an immutable recovery snapshot from the
/// live plan, ask the pure <see cref="IRecoveryEngine"/> for the minimal-perturbation repair, record the
/// absence (Break) for the employee in the scenario, then materialise every reassignment delta (direct
/// covers and swap relocations) as a Replacement WorkChange on the corresponding cloned work (which
/// inherits the scenario token through the WorkChange handler). Locked / uncoverable slots are reported.
/// The proposal is partitioned via <see cref="ICompliancePartitionService"/> (pre-commit guardrail plus
/// the K1 supervisor override); blocked deltas are reported as uncovered, and the non-blocking rule
/// conflicts on the materialised set are surfaced in the outcome for supervised review.
/// </summary>
/// <param name="scenarioRepository">Persists the new AnalyseScenario</param>
/// <param name="scenarioService">Clones the real schedule under the scenario token (with the work id map)</param>
/// <param name="scheduleEntriesService">Reads the absent employee's slots to size the absence Break</param>
/// <param name="snapshotBuilder">Builds the immutable recovery snapshot from the live plan</param>
/// <param name="recoveryEngine">The pure, deterministic re-rostering engine</param>
/// <param name="partitionService">Shared accept/block partition incl. the K1 supervisor override</param>
/// <param name="mediator">Dispatches the Break and Replacement-WorkChange commands</param>
/// <param name="unitOfWork">Flushes the scenario + clone before the slots are read</param>
/// <param name="logger">Logs residual blocking conflicts for supervised review</param>
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.Recovery;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.ScheduleRecovery.Engine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rec = Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class CoverAbsenceCommandHandler : IRequestHandler<CoverAbsenceCommand, CoverAbsenceOutcome>
{
    private const string ScenarioNamePrefix = "Absence cover";
    private const string LockedReason = "locked";
    private const string NoCandidateReason = "no eligible candidate";
    private const string NonCriticalReason = "non-critical";
    private const string BlockedReason = "blocked";
    private const int HoursPerDay = 24;
    private const int MaxAbsenceDays = 31;
    private const decimal DefaultAbsenceHours = 8m;
    private static readonly TimeOnly DayStart = new(0, 0);
    private static readonly TimeOnly DayEnd = new(23, 59);

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IRecoverySnapshotBuilder _snapshotBuilder;
    private readonly IRecoveryEngine _recoveryEngine;
    private readonly ICompliancePartitionService _partitionService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CoverAbsenceCommandHandler> _logger;

    public CoverAbsenceCommandHandler(
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IScheduleEntriesService scheduleEntriesService,
        IRecoverySnapshotBuilder snapshotBuilder,
        IRecoveryEngine recoveryEngine,
        ICompliancePartitionService partitionService,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<CoverAbsenceCommandHandler> logger)
    {
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _scheduleEntriesService = scheduleEntriesService;
        _snapshotBuilder = snapshotBuilder;
        _recoveryEngine = recoveryEngine;
        _partitionService = partitionService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CoverAbsenceOutcome> Handle(CoverAbsenceCommand request, CancellationToken cancellationToken)
    {
        var clientId = request.ClientId;
        var date = request.Date;
        var untilDate = request.UntilDate ?? request.Date;
        var groupId = request.GroupId;
        var absenceId = request.AbsenceId;

        if (untilDate < date)
        {
            throw new ArgumentException($"UntilDate ({untilDate}) must not be before Date ({date}).");
        }

        var totalDays = untilDate.DayNumber - date.DayNumber + 1;
        if (totalDays > MaxAbsenceDays)
        {
            throw new ArgumentException(
                $"Absence spans {totalDays} days; the maximum is {MaxAbsenceDays}. Split into smaller periods.");
        }

        var dates = Enumerable.Range(0, totalDays).Select(offset => date.AddDays(offset)).ToList();

        var token = Guid.NewGuid();
        var name = await GenerateUniqueNameAsync(date, untilDate, groupId, cancellationToken);
        var scenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = date,
            UntilDate = untilDate,
            Token = token,
            RunGroupId = Guid.NewGuid()
        };
        await _scenarioRepository.Add(scenario);

        var (_, workIdMap) = await _scenarioService.CloneScenarioDataWithMapsAsync(
            groupId, date, untilDate, token, additionalShiftIds: null, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var snapshot = await _snapshotBuilder.BuildAsync(groupId, clientId, dates, cancellationToken);
        var proposal = _recoveryEngine.Repair(
            snapshot, new Rec.AbsenceEvent(clientId, dates), Rec.Ruleset.Default);

        await RecordAbsencesAsync(clientId, dates, absenceId, groupId, token, cancellationToken);

        var (materializable, blocked, complianceWarnings) =
            await PartitionDeltasAsync(proposal.Deltas, token, request.OverrideBlock, cancellationToken);
        await MaterialiseMembershipsAsync(proposal, token, cancellationToken);
        await MaterialiseAsync(materializable, workIdMap, cancellationToken);

        var covered = BuildCovered(materializable, clientId, snapshot);
        var uncovered = BuildUncovered(proposal, blocked);
        return new CoverAbsenceOutcome(scenario.Id, token, name, covered, uncovered, complianceWarnings);
    }

    private async Task<decimal> ResolveAbsenceHoursAsync(
        Guid clientId, DateOnly date, Guid groupId, CancellationToken cancellationToken)
    {
        var slots = await _scheduleEntriesService
            .GetScheduleEntriesQuery(date, date, [groupId], null)
            .Where(c => c.EntryType == (int)ScheduleEntryType.Work && c.ClientId == clientId)
            .ToListAsync(cancellationToken);

        return slots.Count > 0
            ? slots.Sum(s => WorkHours(TimeOnly.FromTimeSpan(s.StartTime), TimeOnly.FromTimeSpan(s.EndTime)))
            : DefaultAbsenceHours;
    }

    private async Task MaterialiseAsync(
        IReadOnlyList<Rec.CellDelta> deltas,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        CancellationToken cancellationToken)
    {
        foreach (var delta in deltas)
        {
            // In-group MVP invariant: a snapshot work is backed by exactly one top-level Work
            // (get_schedule_entries returns parent_work_id IS NULL rows; the builder writes [SourceId]).
            if (delta.SourceWorkIds.Count > 1)
            {
                _logger.LogWarning(
                    "Recovery delta for shift {ShiftId} on {Date} is backed by {Count} works; only the first is materialised.",
                    delta.ShiftId, delta.Date, delta.SourceWorkIds.Count);
            }

            var originalWorkId = delta.SourceWorkIds.Count > 0 ? delta.SourceWorkIds[0] : Guid.Empty;
            if (originalWorkId == Guid.Empty || !workIdMap.TryGetValue(originalWorkId, out var clonedWorkId))
            {
                _logger.LogWarning(
                    "Recovery delta for shift {ShiftId} on {Date} has no cloned work to attach to; skipping.",
                    delta.ShiftId, delta.Date);
                continue;
            }

            await _mediator.Send(new PostCommand<WorkChangeResource>(new WorkChangeResource
            {
                WorkId = clonedWorkId,
                Type = WorkChangeType.ReplacementWithin,
                StartTime = TimeOnly.FromDateTime(delta.StartAt),
                EndTime = TimeOnly.FromDateTime(delta.EndAt),
                ChangeTime = delta.Hours,
                ReplaceClientId = delta.ToAgentId,
                Description = RecoveryMarkers.WorkChangeSource
            }), cancellationToken);
        }
    }

    private async Task MaterialiseMembershipsAsync(
        Rec.RecoveryProposal proposal, Guid token, CancellationToken cancellationToken)
    {
        foreach (var membership in proposal.MembershipDeltas)
        {
            await _scenarioService.AddScenarioMembershipAsync(
                token, membership.AgentId, membership.GroupId,
                membership.ValidFrom, membership.ValidUntil, cancellationToken);
        }
    }

    /// <summary>
    /// The engine reasons over a bounded window; the shared partition service sees the full history,
    /// the K1 Block-mode escalation and the supervisor-override path. Only the deltas actually
    /// causing a block are dropped (greedy per violating client); everything else is materialised.
    /// A structural error (collision, missing mandatory qualification) is never overridable.
    /// </summary>
    private async Task<(
        IReadOnlyList<Rec.CellDelta> Materializable,
        IReadOnlyList<Rec.CellDelta> Blocked,
        IReadOnlyList<ScheduleValidationNotificationDto> Warnings)> PartitionDeltasAsync(
        IReadOnlyList<Rec.CellDelta> deltas, Guid token, bool overrideBlockRequested, CancellationToken cancellationToken)
    {
        if (deltas.Count == 0)
        {
            return (deltas, [], []);
        }

        var plannedRows = deltas
            .Select(d => new PlannedWorkRow(
                d.ToAgentId, d.Date, TimeOnly.FromDateTime(d.StartAt), TimeOnly.FromDateTime(d.EndAt), d.ShiftId))
            .ToList();

        var partition = await _partitionService.PartitionAsync(
            plannedRows, token, overrideBlockRequested, cancellationToken);

        var materializable = partition.AcceptedIndexes.Select(i => deltas[i]).ToList();
        var blocked = partition.BlockedIndexes.Select(b => deltas[b.Index]).ToList();

        if (blocked.Count > 0)
        {
            _logger.LogWarning(
                "Recovery proposal for scenario {Token} has {Count} blocking conflict(s) after repair; the affected slot(s) are reported as uncovered instead of committed.",
                token, blocked.Count);
        }

        return (materializable, blocked, partition.ReportableConflicts);
    }

    private static IReadOnlyList<CoveredSlot> BuildCovered(
        IReadOnlyList<Rec.CellDelta> deltas, Guid absentClientId, Rec.RecoverySnapshot snapshot)
    {
        var covered = new List<CoveredSlot>();
        foreach (var delta in deltas)
        {
            if (delta.FromAgentId != absentClientId)
            {
                continue;
            }
            var name = snapshot.FindAgent(delta.ToAgentId)?.DisplayName ?? string.Empty;
            covered.Add(new CoveredSlot(delta.ShiftId ?? Guid.Empty, delta.Date, delta.ToAgentId, name));
        }
        return covered;
    }

    private static IReadOnlyList<UncoveredSlot> BuildUncovered(
        Rec.RecoveryProposal proposal, IReadOnlyList<Rec.CellDelta> blocked)
    {
        var uncovered = new List<UncoveredSlot>();
        foreach (var slot in proposal.Uncovered)
        {
            var reason = slot.Reason switch
            {
                Rec.RecoveryReasons.Locked => LockedReason,
                Rec.RecoveryReasons.NonCritical => NonCriticalReason,
                _ => NoCandidateReason
            };
            uncovered.Add(new UncoveredSlot(slot.ShiftId ?? Guid.Empty, slot.Date, reason));
        }
        foreach (var delta in blocked)
        {
            uncovered.Add(new UncoveredSlot(delta.ShiftId ?? Guid.Empty, delta.Date, BlockedReason));
        }
        return uncovered;
    }

    private async Task RecordAbsencesAsync(
        Guid clientId,
        IReadOnlyList<DateOnly> dates,
        Guid absenceId,
        Guid groupId,
        Guid token,
        CancellationToken cancellationToken)
    {
        var breaks = new List<BulkBreakItem>();
        foreach (var day in dates)
        {
            var hours = await ResolveAbsenceHoursAsync(clientId, day, groupId, cancellationToken);
            breaks.Add(new BulkBreakItem
            {
                ClientId = clientId,
                AbsenceId = absenceId,
                CurrentDate = day,
                StartTime = DayStart,
                EndTime = DayEnd,
                WorkTime = hours,
                AnalyseToken = token
            });
        }

        await _mediator.Send(new BulkAddBreaksCommand(new BulkAddBreaksRequest
        {
            PeriodStart = dates[0],
            PeriodEnd = dates[^1],
            Breaks = breaks
        }), cancellationToken);
    }

    private async Task<string> GenerateUniqueNameAsync(
        DateOnly date, DateOnly untilDate, Guid? groupId, CancellationToken cancellationToken)
    {
        var baseName = untilDate == date
            ? $"{ScenarioNamePrefix} {date:dd.MM.yy}"
            : $"{ScenarioNamePrefix} {date:dd.MM.yy}–{untilDate:dd.MM.yy}";
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
