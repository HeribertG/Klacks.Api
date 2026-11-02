// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Writes the accepted repair of an absence cover into its scenario. A reassignment that is the only one of its work and
/// takes the whole work moves the work (CoverMoveDecision, IWorkHandOverService), relocation
/// hops of a swap included; every other one stays a Replacement WorkChange through PostCommand. After a move the overtime
/// successors and the period hours of both people run in the order of the Work handlers: commit, successors, hours. When
/// the recalculation for the new owner fails, the staged move is dropped and the work stays a replacement, so its
/// surcharges stay with the person who owns it.
/// </summary>
/// <param name="mediator">Dispatches the Replacement WorkChange commands</param>
/// <param name="factsReader">Reads what the decision needs about the scenario works</param>
/// <param name="workRepository">Loads the work to move</param>
/// <param name="handOverService">Moves the work</param>
/// <param name="periodHoursService">Resolves the period of the moved work</param>
/// <param name="unitOfWork">Commits the move inside the transaction of the cover, drops a move that failed</param>
/// <param name="overtimeCascadeService">Reprocesses the overtime successors of the old and the new owner</param>
/// <param name="completionService">Recalculates the period hours of both people</param>
/// <param name="logger">Logs reassignments that cannot be written as proposed</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.ScheduleRecovery.Model;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public sealed class AbsenceCoverMaterializer : IAbsenceCoverMaterializer
{
    private const string WorkNotFoundMessage = "Work with ID {0} not found.";

    private readonly IMediator _mediator;
    private readonly ICoverMoveFactsReader _factsReader;
    private readonly ICompanyClock _companyClock;
    private readonly IWorkRepository _workRepository;
    private readonly IWorkHandOverService _handOverService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IScheduleCompletionService _completionService;
    private readonly ILogger<AbsenceCoverMaterializer> _logger;

    public AbsenceCoverMaterializer(
        IMediator mediator,
        ICoverMoveFactsReader factsReader,
        ICompanyClock companyClock,
        IWorkRepository workRepository,
        IWorkHandOverService handOverService,
        IPeriodHoursService periodHoursService,
        IUnitOfWork unitOfWork,
        IOvertimeCascadeService overtimeCascadeService,
        IScheduleCompletionService completionService,
        ILogger<AbsenceCoverMaterializer> logger)
    {
        _mediator = mediator;
        _factsReader = factsReader;
        _companyClock = companyClock;
        _workRepository = workRepository;
        _handOverService = handOverService;
        _periodHoursService = periodHoursService;
        _unitOfWork = unitOfWork;
        _overtimeCascadeService = overtimeCascadeService;
        _completionService = completionService;
        _logger = logger;
    }

    public async Task<CoverSourceWorks> FindSourceWorksAsync(
        IReadOnlyDictionary<Guid, Guid> workIdMap, Guid analyseToken, CoverReport report, CancellationToken cancellationToken)
    {
        var facts = await _factsReader.LoadAsync(workIdMap.Values.ToList(), analyseToken, cancellationToken);
        var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var known = workIdMap.Where(work => facts.ContainsKey(work.Value)).ToList();
        return new CoverSourceWorks(
            known.Where(work => CoverMoveDecision.MayMove(facts[work.Value], report, companyTimeZone)).Select(work => work.Key).ToHashSet(),
            known.Where(work => facts[work.Value].IsMoved).Select(work => work.Key).ToHashSet());
    }

    public async Task<CoverMaterialisationResult> MaterialiseAsync(CoverMaterialisation materialisation, CancellationToken cancellationToken)
    {
        var slots = ResolveSlots(materialisation);
        var moved = await SelectMovesAsync(slots, materialisation, cancellationToken);
        var relocationsKeepingExtras = new List<CellDelta>();

        foreach (var (delta, clonedWorkId) in slots)
        {
            if (moved.Contains(clonedWorkId) && await TryMoveAsync(delta, clonedWorkId, cancellationToken))
            {
                continue;
            }

            moved.Remove(clonedWorkId);
            await ReplaceAsync(delta, clonedWorkId, cancellationToken);
            if (delta.MovesAsWhole && delta.ExtraPaidHours > 0m)
            {
                relocationsKeepingExtras.Add(delta);
            }
        }

        return new CoverMaterialisationResult(moved, relocationsKeepingExtras);
    }

    /// <summary>
    /// In-group MVP invariant: a snapshot work is backed by exactly one top-level Work (get_schedule_entries returns
    /// parent_work_id IS NULL rows; the builder writes [SourceId]). Only the first work of a delta is written.
    /// </summary>
    private List<(CellDelta Delta, Guid ClonedWorkId)> ResolveSlots(CoverMaterialisation materialisation)
    {
        var slots = new List<(CellDelta, Guid)>(materialisation.Deltas.Count);
        foreach (var delta in materialisation.Deltas)
        {
            if (delta.SourceWorkIds.Count > 1)
            {
                _logger.LogWarning(
                    "Recovery delta for shift {ShiftId} on {Date} is backed by {Count} works; only the first is materialised.",
                    delta.ShiftId, delta.Date, delta.SourceWorkIds.Count);
            }

            var originalWorkId = delta.SourceWorkIds.Count > 0 ? delta.SourceWorkIds[0] : Guid.Empty;
            if (originalWorkId == Guid.Empty || !materialisation.WorkIdMap.TryGetValue(originalWorkId, out var clonedWorkId))
            {
                _logger.LogWarning(
                    "Recovery delta for shift {ShiftId} on {Date} has no cloned work to attach to; skipping.",
                    delta.ShiftId, delta.Date);
                continue;
            }

            slots.Add((delta, clonedWorkId));
        }

        return slots;
    }

    private async Task<HashSet<Guid>> SelectMovesAsync(
        List<(CellDelta Delta, Guid ClonedWorkId)> slots, CoverMaterialisation materialisation, CancellationToken cancellationToken)
    {
        var candidates = slots
            .GroupBy(slot => slot.ClonedWorkId)
            .Where(work => work.Count() == 1 && work.Single().Delta.SourceWorkIds.Count == 1)
            .Select(work => work.Single())
            .ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var facts = await _factsReader.LoadAsync(
            candidates.Select(slot => slot.ClonedWorkId).ToList(), materialisation.AnalyseToken, cancellationToken);
        var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);

        return candidates
            .Where(slot => facts.TryGetValue(slot.ClonedWorkId, out var work)
                && CoverMoveDecision.MayMove(work, materialisation.Report, companyTimeZone)
                && CoverMoveDecision.CoversWholeWork(
                    work, slot.Delta.FromAgentId, TimeOnly.FromDateTime(slot.Delta.StartAt), TimeOnly.FromDateTime(slot.Delta.EndAt)))
            .Select(slot => slot.ClonedWorkId)
            .ToHashSet();
    }

    private async Task<bool> TryMoveAsync(CellDelta delta, Guid clonedWorkId, CancellationToken cancellationToken)
    {
        await _unitOfWork.CompleteAsync();

        var previous = await _workRepository.GetNoTracking(clonedWorkId)
            ?? throw new KeyNotFoundException(string.Format(WorkNotFoundMessage, clonedWorkId));
        var work = await _workRepository.Get(clonedWorkId)
            ?? throw new KeyNotFoundException(string.Format(WorkNotFoundMessage, clonedWorkId));
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

        if (!await _handOverService.TransferAsync(work, delta.ToAgentId, delta.FromAgentId, cancellationToken))
        {
            _unitOfWork.DiscardTrackedChanges();
            _logger.LogWarning(
                "Moving work {WorkId} failed in the recalculation for the new owner; it stays a replacement.", clonedWorkId);
            return false;
        }

        await _unitOfWork.CompleteAsync();
        await _overtimeCascadeService.ReprocessSuccessorsAsync(work, previous);
        await _completionService.SaveAndTrackMoveAsync(
            work.ClientId, work.CurrentDate, periodStart, periodEnd, previous.ClientId, previous.CurrentDate, work.AnalyseToken);
        return true;
    }

    private async Task ReplaceAsync(CellDelta delta, Guid clonedWorkId, CancellationToken cancellationToken)
    {
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
