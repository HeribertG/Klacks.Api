// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Ends a real shift family on one day. The head of the family is its sealed order, or without an order the addressed row
/// itself: a family without an order is a container, one row. A row whose OriginalId leads to no sealed order is refused.
/// The stored end of a sealed order is the end of its family; a container has ended once its row has an end
/// (ShiftFamilyRules). The head row is locked first, so two saves cannot both pass the set-once check, and everything is read
/// after that lock. The tracker is emptied before each run of the transaction: on a database
/// retry the tracked rows would otherwise still hold the unsaved end date of the failed run and pass the check. The first
/// check is the group visibility of every family row, the order included, because the end date writes them all and the
/// later refusals would tell dates of hidden rows; a caller who does not see the whole family changes nothing. Refused
/// before anything is written: a day before the start of the family or of the addressed row, a head or addressed row that already ends on another day, any real work after the day,
/// any work after the day on a live scenario clone of a family row (an accept would turn it real behind the end date),
/// and a part starting after the day that is still used. Then every row without an end or with a later end gets the day,
/// and the parts that start after the day are soft-deleted with their subtree. The trees are numbered in a second save,
/// because the numbering reads the database and must no longer see the removed rows.
/// </summary>
/// <param name="shiftRepository">Stages the removal of the parts that start after the day</param>
/// <param name="orderFamilyEndRepository">Locks and reads the family, and finds the last work after the day</param>
/// <param name="shiftTreeService">Numbers the trees that lost a part</param>
/// <param name="shiftVisibilityGuard">Decides whether the caller sees every row of the family</param>
/// <param name="unitOfWork">Runs the transaction and commits</param>
/// <param name="logger">Logs a refusal and the removal of parts</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Services.Shifts;

public class ShiftFamilyEndService : IShiftFamilyEndService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderFamilyEndRepository _orderFamilyEndRepository;
    private readonly IShiftTreeService _shiftTreeService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShiftFamilyEndService> _logger;

    public ShiftFamilyEndService(
        IShiftRepository shiftRepository,
        IOrderFamilyEndRepository orderFamilyEndRepository,
        IShiftTreeService shiftTreeService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<ShiftFamilyEndService> logger)
    {
        _shiftRepository = shiftRepository;
        _orderFamilyEndRepository = orderFamilyEndRepository;
        _shiftTreeService = shiftTreeService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task EndFamilyAsync(Shift entry, DateOnly untilDate, Func<Task>? saveEntry, CancellationToken cancellationToken)
    {
        var headId = ShiftFamilyRules.HeadIdOf(entry);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _unitOfWork.DiscardTrackedChanges();
            await _orderFamilyEndRepository.LockHeadAsync(headId, cancellationToken);
            var family = await _orderFamilyEndRepository.GetTrackedFamilyAsync(headId, cancellationToken);

            var plan = await CheckAndPlanAsync(entry, family, headId, untilDate, cancellationToken);

            if (saveEntry != null)
            {
                await saveEntry();
            }

            await ApplyAsync(plan, untilDate, cancellationToken);
            return headId;
        });
    }

    private async Task<ShiftFamilyEndPlan> CheckAndPlanAsync(
        Shift entry, IReadOnlyList<Shift> family, Guid headId, DateOnly untilDate, CancellationToken cancellationToken)
    {
        if (await _shiftVisibilityGuard.FindFirstHiddenAsync(family.Select(row => row.Id).ToList(), cancellationToken) != null)
        {
            throw Refused(ShiftEndDateRefusedException.ForHiddenRows(), entry);
        }

        var head = family.FirstOrDefault(row => row.Id == headId
                && (row.Status == ShiftStatus.SealedOrder || (entry.OriginalId == null && row.OriginalId == null)))
            ?? throw Refused(ShiftEndDateRefusedException.ForMissingOrder(), entry);

        var start = head.FromDate > entry.FromDate ? head.FromDate : entry.FromDate;
        if (untilDate < start)
        {
            throw Refused(ShiftEndDateRefusedException.ForBeforeStart(untilDate, start), entry);
        }

        var familyEnd = head.Status == ShiftStatus.SealedOrder ? head.UntilDate : ShiftFamilyRules.StoredEndWithoutOrder(family);
        var storedEnd = familyEnd ?? family.FirstOrDefault(row => row.Id == entry.Id)?.UntilDate;
        if (storedEnd is { } end && end != untilDate)
        {
            throw Refused(ShiftEndDateRefusedException.ForStoredEnd(untilDate, end), entry);
        }

        var lastWork = await _orderFamilyEndRepository.FindLastWorkDateAfterAsync(headId, untilDate, cancellationToken);
        if (lastWork is { } lastWorkDate)
        {
            throw Refused(ShiftEndDateRefusedException.ForWorksAfter(untilDate, lastWorkDate), entry);
        }

        var lastScenarioWork = await _orderFamilyEndRepository.FindLastScenarioWorkDateAfterAsync(headId, untilDate, cancellationToken);
        if (lastScenarioWork.HasValue)
        {
            throw Refused(ShiftEndDateRefusedException.ForScenarioWorksAfter(untilDate), entry);
        }

        var plan = ShiftFamilyEndPlanner.Build(family, headId, untilDate);
        if (plan.RowsToRemove.Count > 0)
        {
            var usage = await _orderFamilyEndRepository.GetUsageAsync(plan.RowsToRemove.Select(row => row.Id).ToList(), cancellationToken);
            if (usage.IsInUse)
            {
                throw Refused(ShiftEndDateRefusedException.ForRowsInUse(untilDate), entry);
            }
        }

        return plan;
    }

    private async Task ApplyAsync(ShiftFamilyEndPlan plan, DateOnly untilDate, CancellationToken cancellationToken)
    {
        foreach (var row in plan.RowsToTrim)
        {
            row.UntilDate = untilDate;
        }

        if (plan.RowsToRemove.Count == 0)
        {
            await _unitOfWork.CompleteAsync();
            return;
        }

        var removedIds = plan.RowsToRemove.Select(row => row.Id).ToList();
        var treeRootIds = plan.RowsToRemove
            .Where(row => row.RootId.HasValue)
            .Select(row => row.RootId!.Value)
            .Distinct()
            .ToList();

        await _shiftRepository.DeleteOrderFamilyAsync(removedIds, cancellationToken);
        await _unitOfWork.CompleteAsync();

        await _shiftTreeService.RecalculateTreesAsync(treeRootIds, cancellationToken);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Family end removed {RowCount} part(s) that start after {UntilDate}", removedIds.Count, untilDate);
    }

    private ShiftEndDateRefusedException Refused(ShiftEndDateRefusedException refusal, Shift entry)
    {
        _logger.LogWarning("End date refused: Id={ShiftId}, Reason={Reason}",
            entry.Id, refusal.ConflictDetails?[ShiftEndDateRefusedException.ReasonKey]);
        return refusal;
    }
}
