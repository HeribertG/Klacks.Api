// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Puts a cut family back to the state at creation: the sealed row as it is. The head row is locked first and everything,
/// the sealed row included, is read after that lock; the tracker is emptied before each run of the transaction, so a database retry does not see the
/// unsaved changes of the failed run. A caller who does not see every row of the family gets the answer of a missing order,
/// before any date is compared. Refused before anything is written: a real work on or after the start, a start behind the end of the sealed order, a work on or after the
/// start on a live scenario clone of a family row (an accept would turn it real), a family that has its plannable shift and
/// no cut piece from the start on, a kept row that starts on or after the start, and a row to remove that is still used by a
/// work, a scenario clone or a container. A scenario clone of a kept row without such a work does not block. Then the kept rows
/// end the day before the start, a new plannable shift takes content, groups, requirements and fresh copies of the default
/// expenses of the sealed order, and every other row is soft-deleted with its subtree. The trees are numbered in a second
/// save, because the numbering reads the database and must no longer see the removed rows. A tree inside a variant gets
/// no numbers: they are set when the variant is promoted. A variant has no works, so it always goes back to its start.
/// </summary>
/// <param name="shiftRepository">Reads the rows below the order and stages the removal</param>
/// <param name="orderFamilyEndRepository">Locks the head and finds works and the usage of rows</param>
/// <param name="shiftExpensesRepository">Reads the stored default expenses of the sealed order</param>
/// <param name="shiftResetService">Stages the new plannable shift</param>
/// <param name="shiftTreeService">Numbers the trees that lost a row</param>
/// <param name="shiftVisibilityGuard">Decides whether the caller sees every row of the family</param>
/// <param name="unitOfWork">Runs the transaction and commits</param>
/// <param name="logger">Logs a refusal and the removal of rows</param>

using System.Globalization;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Services.Schedules;

public class ShiftFamilyReset : IShiftFamilyReset
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderFamilyEndRepository _orderFamilyEndRepository;
    private readonly IShiftExpensesRepository _shiftExpensesRepository;
    private readonly IShiftResetService _shiftResetService;
    private readonly IShiftTreeService _shiftTreeService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShiftFamilyReset> _logger;

    public ShiftFamilyReset(
        IShiftRepository shiftRepository,
        IOrderFamilyEndRepository orderFamilyEndRepository,
        IShiftExpensesRepository shiftExpensesRepository,
        IShiftResetService shiftResetService,
        IShiftTreeService shiftTreeService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IUnitOfWork unitOfWork,
        ILogger<ShiftFamilyReset> logger)
    {
        _shiftRepository = shiftRepository;
        _orderFamilyEndRepository = orderFamilyEndRepository;
        _shiftExpensesRepository = shiftExpensesRepository;
        _shiftResetService = shiftResetService;
        _shiftTreeService = shiftTreeService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<Shift>> ResetAsync(Shift order, DateOnly requestedStart)
        => _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _unitOfWork.DiscardTrackedChanges();
            await _orderFamilyEndRepository.LockHeadAsync(order.Id);
            var sealedOrder = await _shiftRepository.GetSealedOrder(order.Id, order.AnalyseToken)
                ?? throw new InvalidOperationException(string.Format(ShiftVisibilityMessages.NoSealedOrderFound, order.Id));
            var rows = await _shiftRepository.CutList(sealedOrder.Id, tracked: true, analyseToken: sealedOrder.AnalyseToken);

            if (await _shiftVisibilityGuard.FindFirstHiddenAsync(rows.Select(row => row.Id).ToList()) != null)
            {
                throw new InvalidOperationException(string.Format(ShiftVisibilityMessages.NoSealedOrderFound, sealedOrder.Id));
            }

            if (rows.Count == 0)
            {
                return rows;
            }

            var start = await ResolveStartAsync(sealedOrder, requestedStart, rows);
            var plan = await CheckAndPlanAsync(sealedOrder.Id, rows, start);
            await ApplyAsync(sealedOrder, start, plan);

            return await _shiftRepository.CutList(sealedOrder.Id, analyseToken: sealedOrder.AnalyseToken);
        });

    private async Task<DateOnly> ResolveStartAsync(Shift sealedOrder, DateOnly requestedStart, List<Shift> rows)
    {
        var lastWork = await _orderFamilyEndRepository.FindLastWorkDateAfterAsync(sealedOrder.Id, DateOnly.MinValue);
        if (lastWork is { } lastWorkDate && lastWorkDate >= requestedStart)
        {
            throw Refused(ShiftResetRefusedException.ForWorksOnOrAfter(requestedStart, lastWorkDate), sealedOrder.Id);
        }

        var start = ShiftResetRules.EffectiveStart(sealedOrder.FromDate, lastWork, requestedStart);
        if (sealedOrder.UntilDate is { } orderEnd && start > orderEnd)
        {
            throw new BadRequestException(string.Format(
                CultureInfo.InvariantCulture,
                ShiftHierarchyMessages.ResetBehindOrderEnd,
                start.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                orderEnd.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)));
        }

        var lastScenarioWork = await _orderFamilyEndRepository.FindLastScenarioWorkDateAfterAsync(sealedOrder.Id, start.AddDays(-1));
        if (lastScenarioWork.HasValue)
        {
            throw Refused(ShiftResetRefusedException.ForScenarioWorksOnOrAfter(start), sealedOrder.Id);
        }

        if (ShiftResetRules.HasNothingToReset(rows, start))
        {
            throw Refused(ShiftResetRefusedException.ForNothingToReset(start), sealedOrder.Id);
        }

        return start;
    }

    private async Task<ShiftResetPlan> CheckAndPlanAsync(Guid headId, List<Shift> rows, DateOnly start)
    {
        var idsWithWork = await _orderFamilyEndRepository.FindShiftIdsWithRealWorkAsync(rows.Select(row => row.Id).ToList());
        var plan = ShiftResetRules.Plan(rows, idsWithWork, start);

        if (plan.HasKeptRowFromStart)
        {
            throw Refused(ShiftResetRefusedException.ForRowsInUse(), headId);
        }

        if (plan.RowsToRemove.Count > 0)
        {
            var usage = await _orderFamilyEndRepository.GetUsageAsync(plan.RowsToRemove.Select(row => row.Id).ToList());
            if (usage.IsInUse)
            {
                throw Refused(ShiftResetRefusedException.ForRowsInUse(), headId);
            }
        }

        return plan;
    }

    private async Task ApplyAsync(Shift sealedOrder, DateOnly start, ShiftResetPlan plan)
    {
        var closeDate = start.AddDays(-1);
        foreach (var row in plan.RowsToClose)
        {
            row.UntilDate = closeDate;
        }

        var expenses = await _shiftExpensesRepository.GetByShiftIdsAsync([sealedOrder.Id]);
        await _shiftResetService.CreateNewOriginalShiftFromSealedOrderAsync(sealedOrder, start, expenses);

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

        await _shiftRepository.DeleteOrderFamilyAsync(removedIds);
        await _unitOfWork.CompleteAsync();

        if (sealedOrder.AnalyseToken == null)
        {
            await _shiftTreeService.RecalculateTreesAsync(treeRootIds, CancellationToken.None);
            await _unitOfWork.CompleteAsync();
        }

        _logger.LogInformation("Reset removed {RowCount} row(s) of order {OrderId}", removedIds.Count, sealedOrder.Id);
    }

    private ShiftResetRefusedException Refused(ShiftResetRefusedException refusal, Guid orderId)
    {
        _logger.LogWarning("Reset refused: OrderId={OrderId}, Reason={Reason}",
            orderId, refusal.ConflictDetails?[ShiftResetRefusedException.ReasonKey]);
        return refusal;
    }
}
