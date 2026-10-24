// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Executes the review decisions for one order in one transaction. 1) The order is changed: a new start of a full-day
/// order moves start and end together (still 24h), a first group is linked from the company's today, the chosen
/// qualifications are added as mandatory requirements (lowest level), the expenses are added as default expenses; the
/// change is saved BEFORE sealing, because sealing copies the required qualifications from the database. 2) The order
/// is checked and sealed with the sealing core inside this transaction (IOrderSealingService.SealWithinTransactionAsync).
/// 3) When a split was decided, the new plannable shift is cut through the cut_shift planner and the cut facade; the
/// planned parts must be exactly the decided ones, and every part carries the default expenses (ShiftCutFacade). Any
/// refusal or exception rolls the whole order back, clears the change tracker and is returned as a failure.
/// </summary>
/// <param name="shiftRepository">Writes the changed order.</param>
/// <param name="unitOfWork">Opens the per-order transaction and saves the change before sealing.</param>
/// <param name="orderSealingService">Checks and seals the changed order without a transaction of its own.</param>
/// <param name="cutPlanner">Plans the cut of the plannable shift into the decided parts.</param>
/// <param name="cutFacade">Writes the cut.</param>
/// <param name="companyClock">The company's today, the start of a new group link.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Orders.Review;

public sealed class OrderReviewExecutor : IOrderReviewExecutor
{
    private const string OperationName = "execute_order_review";
    private const string PartSeparator = ",";
    private const string RangeSeparator = "-";
    private const string OrderVanishedFormat = "Order '{0}' could not be changed - it was not found any more.";
    private const string CutMismatchFormat =
        "The cut of '{0}' would not produce the decided parts ({1}); nothing was changed.";
    private const string CutCountFormat = "The cut of '{0}' wrote {1} part(s) instead of {2}; nothing was changed.";

    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderSealingService _orderSealingService;
    private readonly IShiftTimeCutPlanner _cutPlanner;
    private readonly IShiftCutFacade _cutFacade;
    private readonly ICompanyClock _companyClock;

    public OrderReviewExecutor(
        IShiftRepository shiftRepository,
        IUnitOfWork unitOfWork,
        IOrderSealingService orderSealingService,
        IShiftTimeCutPlanner cutPlanner,
        IShiftCutFacade cutFacade,
        ICompanyClock companyClock)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
        _orderSealingService = orderSealingService;
        _cutPlanner = cutPlanner;
        _cutFacade = cutFacade;
        _companyClock = companyClock;
    }

    public async Task<OrderReviewExecutionOutcome> ExecuteAsync(
        OrderReviewExecutionTarget target, string userName, string? language, CancellationToken cancellationToken = default)
    {
        var item = target.Item;
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        try
        {
            var (plannableShiftId, partCount) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await ChangeOrderAsync(target.Order, item, today, userName);
                var plannable = await SealAsync(item, cancellationToken);
                var parts = await CutAsync(plannable, item, language, cancellationToken);
                return (plannable, parts);
            });

            return new OrderReviewExecutionOutcome(item.OrderId, true, plannableShiftId, partCount, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _unitOfWork.DiscardTrackedChanges();
            return new OrderReviewExecutionOutcome(item.OrderId, false, null, 0, ex.Message);
        }
    }

    private async Task ChangeOrderAsync(Shift order, OrderReviewDraftItem item, DateTime today, string userName)
    {
        if (item.IsFullDay && OrderReviewReadiness.ChangesStart(item))
        {
            order.StartShift = item.ChosenStart!.Value;
            order.EndShift = item.ChosenStart!.Value;
        }

        if (!item.HasGroup && item.ChosenGroupId.HasValue
            && order.GroupItems.All(link => link.IsDeleted || link.GroupId != item.ChosenGroupId.Value))
        {
            order.GroupItems.Add(new GroupItem
            {
                Id = Guid.NewGuid(),
                ShiftId = order.Id,
                GroupId = item.ChosenGroupId.Value,
                ValidFrom = today,
                CurrentUserCreated = userName,
            });
        }

        foreach (var qualificationId in (item.ChosenQualificationIds ?? [])
                     .Where(id => order.RequiredQualifications.All(requirement => requirement.IsDeleted || requirement.QualificationId != id)))
        {
            order.RequiredQualifications.Add(new ShiftRequiredQualification
            {
                Id = Guid.NewGuid(),
                ShiftId = order.Id,
                QualificationId = qualificationId,
                IsMandatory = true,
                MinLevel = OrderReviewDefaults.RequiredQualificationLevel,
            });
        }

        foreach (var expense in item.Expenses)
        {
            order.ShiftExpenses.Add(new ShiftExpenses
            {
                Id = Guid.NewGuid(),
                ShiftId = order.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                Taxable = expense.Taxable,
            });
        }

        if (await _shiftRepository.Put(order) == null)
        {
            throw new SkillVerificationException(OperationName, string.Format(OrderVanishedFormat, item.OrderName));
        }

        await _unitOfWork.CompleteAsync();
    }

    private async Task<Guid> SealAsync(OrderReviewDraftItem item, CancellationToken cancellationToken)
    {
        var check = await _orderSealingService.CheckAsync(item.OrderId, cancellationToken);
        if (check.Refusal != null)
        {
            throw new SkillVerificationException(OperationName, check.Refusal.FailureReason!);
        }

        return await _orderSealingService.SealWithinTransactionAsync(check.SealableOrder!, cancellationToken);
    }

    private async Task<int> CutAsync(Guid plannableShiftId, OrderReviewDraftItem item, string? language, CancellationToken cancellationToken)
    {
        if (item.ChosenParts is not { Count: > 0 } parts)
        {
            return 0;
        }

        var partsRaw = string.Join(PartSeparator, parts.Select(part =>
            OrderReviewPresenter.Time(part.Start) + RangeSeparator + OrderReviewPresenter.Time(part.End)));
        var namesRaw = string.Join(PartSeparator, parts.Select(part => part.Name));
        var plan = await _cutPlanner.PlanAsync(plannableShiftId, partsRaw, namesRaw, language, cancellationToken);
        if (plan.IsRefused)
        {
            throw new SkillVerificationException(OperationName, plan.Refusal!);
        }

        var planned = plan.Parts.Select(part => (part.Start, part.End)).ToList();
        if (!planned.SequenceEqual(parts.Select(part => (part.Start, part.End))))
        {
            throw new SkillVerificationException(
                OperationName, string.Format(CultureInfo.InvariantCulture, CutMismatchFormat, item.OrderName, partsRaw));
        }

        var written = await _cutFacade.ProcessBatchCutsAsync(plan.Operations.ToList());
        if (written.Count != parts.Count)
        {
            throw new SkillVerificationException(
                OperationName, string.Format(CultureInfo.InvariantCulture, CutCountFormat, item.OrderName, written.Count, parts.Count));
        }

        return written.Count;
    }
}
