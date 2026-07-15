// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IOvertimeCascadeService (K3/K4). After a Work add/edit/delete has been committed, finds the
/// sibling Works of the same client and basis period that sort after the changed Work in the
/// deterministic (CurrentDate, StartTime, Id) partition order, reruns their macro/overtime processing
/// (their prior-hours sum changed) and persists the result in one extra save. Predecessors are never
/// touched (their prior sums are unaffected), sealed successors (LockLevel != None) are skipped with a
/// warning, and the reprocessing itself never fans out again — a successor's recomputation only changes
/// its own surcharge items, not the period's hour total, so a single pass keeps the partition exact.
/// </summary>
/// <param name="overtimeSurchargeCalculator">Resolves the overtime configuration and the successor Works per basis period</param>
/// <param name="workMacroService">Reruns the per-Work macro/overtime processing for each successor</param>
/// <param name="unitOfWork">Persists the reprocessed successors after the cascade</param>
/// <param name="logger">Warnings for skipped sealed Works</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class OvertimeCascadeService : IOvertimeCascadeService
{
    private readonly IOvertimeSurchargeCalculator _overtimeSurchargeCalculator;
    private readonly IWorkMacroService _workMacroService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OvertimeCascadeService> _logger;

    public OvertimeCascadeService(
        IOvertimeSurchargeCalculator overtimeSurchargeCalculator,
        IWorkMacroService workMacroService,
        IUnitOfWork unitOfWork,
        ILogger<OvertimeCascadeService> logger)
    {
        _overtimeSurchargeCalculator = overtimeSurchargeCalculator;
        _workMacroService = workMacroService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ReprocessSuccessorsAsync(Work work, Work? previousState = null)
    {
        var anchors = new List<Work> { work };
        if (previousState is not null && AffectsDifferentSuccessors(work, previousState))
        {
            anchors.Add(previousState);
        }

        await ReprocessAnchorsAsync(anchors);
    }

    public async Task ReprocessSuccessorsAsync(IReadOnlyCollection<Work> works)
    {
        await ReprocessAnchorsAsync(works);
    }

    private async Task ReprocessAnchorsAsync(IReadOnlyCollection<Work> anchors)
    {
        var successorsById = new Dictionary<Guid, Work>();
        foreach (var anchor in DeduplicateAnchors(anchors))
        {
            var successors = await _overtimeSurchargeCalculator.GetSuccessorWorksAsync(anchor);
            foreach (var successor in successors)
            {
                successorsById.TryAdd(successor.Id, successor);
            }
        }

        if (successorsById.Count == 0)
        {
            return;
        }

        var reprocessedCount = 0;
        foreach (var successor in successorsById.Values)
        {
            if (successor.LockLevel != WorkLockLevel.None)
            {
                _logger.LogWarning(
                    "Skipping overtime cascade for sealed Work {WorkId} (LockLevel={LockLevel}); its overtime tier bands stay stale until it is unsealed and reprocessed",
                    successor.Id,
                    successor.LockLevel);
                continue;
            }

            await _workMacroService.ProcessWorkMacroAsync(successor);
            reprocessedCount++;
        }

        if (reprocessedCount > 0)
        {
            await _unitOfWork.CompleteAsync();
        }
    }

    /// <summary>
    /// Collapses anchors that share (ClientId, AnalyseToken, CurrentDate) to the earliest one in the
    /// (StartTime, Id) order: its successor set is a superset of every later same-day anchor's set, so
    /// one successor query per client and day suffices for bulk operations regardless of whether the
    /// overtime basis resolves to day or week.
    /// </summary>
    private static IEnumerable<Work> DeduplicateAnchors(IReadOnlyCollection<Work> anchors)
    {
        return anchors
            .GroupBy(a => (a.ClientId, a.AnalyseToken, a.CurrentDate))
            .Select(g => g.OrderBy(a => a.StartTime).ThenBy(a => a.Id).First());
    }

    private static bool AffectsDifferentSuccessors(Work current, Work previous)
    {
        return current.ClientId != previous.ClientId
            || current.CurrentDate != previous.CurrentDate
            || current.StartTime != previous.StartTime
            || current.AnalyseToken != previous.AnalyseToken;
    }
}
