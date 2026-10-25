// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one sealing path for an order, extracted from SealShiftSkill so seal_shift and seal_open_orders
/// share it instead of carrying two sealing algorithms. Sealing is one transaction per order: the status
/// moves OriginalOrder to SealedOrder, PutWithSealedOrderHandling clones the row into the plannable
/// OriginalShift (carrying its group items and expenses, and copying its required qualifications), and
/// both rows are re-read before the commit so a write that did not land rolls back instead of being
/// reported as a success. There is no counterpart operation: a sealed order can never be unsealed.
/// seal_shift may hand over a group to attach (groupToAttach, the group derived from the customer's address for an order
/// without a group): it then satisfies the group requirement, is added to the order inside the same transaction (valid
/// from the order's start, so the clone carries it to the plannable shift) and is verified on the plannable shift after
/// the commit. An order that has a group of its own by then is refused instead of getting a second one. Without a group to
/// attach every path (seal_open_orders, the UI) behaves exactly as before.
/// </summary>
/// <param name="shiftRepository">Loads the order and performs the sealing write and its verification read.</param>
/// <param name="unitOfWork">Opens the per-order transaction the whole transition runs in.</param>
/// <param name="shiftVisibilityGuard">Answers an order a group-restricted caller may not see exactly like a missing one.</param>

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Orders;

public sealed class OrderSealingService : IOrderSealingService
{
    private const string OperationName = "seal_shift";

    private const string NotFoundFormat = "Order with ID {0} not found.";

    private const string AlreadySealedFormat =
        "Order '{0}' is already sealed (status SealedOrder) and cannot be sealed again. " +
        "Use search_shifts or get_shift_details to find its plannable shift.";

    private const string NotAnOrderFormat =
        "Shift '{0}' has status {1} — it is a plannable shift, not an order. " +
        "Only an order (status OriginalOrder) can be sealed.";

    private const string MissingRequirementsFormat =
        "Cannot seal order '{0}' yet — missing/invalid: {1}. " +
        "Complete these fields on the order first, then call seal_shift again.";

    private const string OrderVanishedFormat =
        "Order '{0}' could not be found while sealing — the write was rolled back.";

    private const string VerificationFailedFormat =
        "Database verification failed: {0} could not be confirmed in the database " +
        "after the write — the change was rolled back.";

    private const string GroupAppearedFormat =
        "Order '{0}' got a group of its own since the preview, so the group derived from the customer's address is not " +
        "attached and nothing was sealed. Repeat seal_shift without a confirmation token to get a fresh preview.";

    private const string GroupLinkDescriptionFormat = "the group link of the plannable shift created for sealed order '{0}'";

    private const string SealedOrderDescriptionFormat = "the sealing of order '{0}'";

    private const string PlannableShiftDescriptionFormat =
        "the plannable shift created for sealed order '{0}'";

    private const string MissingAbbreviation = "abbreviation";
    private const string MissingName = "name";
    private const string MissingFromDate = "fromDate";
    private const string MissingWeekday = "at least one weekday or holiday flag";
    internal const string MissingGroupRequirement = "at least one group";
    private const string MissingQuantity = "quantity > 0";
    private const string MissingSumEmployees = "sumEmployees > 0";

    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public OrderSealingService(
        IShiftRepository shiftRepository, IUnitOfWork unitOfWork, IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public IReadOnlyList<string> CollectMissingRequirements(Shift order)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(order.Abbreviation))
        {
            missing.Add(MissingAbbreviation);
        }

        if (string.IsNullOrWhiteSpace(order.Name))
        {
            missing.Add(MissingName);
        }

        if (order.FromDate == default)
        {
            missing.Add(MissingFromDate);
        }

        var hasAnyWeekdaySelected = order.IsMonday || order.IsTuesday || order.IsWednesday
            || order.IsThursday || order.IsFriday || order.IsSaturday || order.IsSunday || order.IsHoliday;
        if (!hasAnyWeekdaySelected)
        {
            missing.Add(MissingWeekday);
        }

        if (!order.GroupItems.Any(gi => !gi.IsDeleted))
        {
            missing.Add(MissingGroupRequirement);
        }

        if (order.Quantity <= 0)
        {
            missing.Add(MissingQuantity);
        }

        if (order.SumEmployees <= 0)
        {
            missing.Add(MissingSumEmployees);
        }

        return missing;
    }

    public Task<OrderSealingCheck> CheckAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        CheckAsync(orderId, groupToAttach: null, cancellationToken);

    public async Task<OrderSealingCheck> CheckAsync(
        Guid orderId, Guid? groupToAttach, CancellationToken cancellationToken = default)
    {
        var order = await _shiftRepository.Get(orderId);
        if (order == null || !await _shiftVisibilityGuard.IsVisibleAsync(order, cancellationToken))
        {
            return new OrderSealingCheck(null, Refused(orderId, string.Empty, string.Format(NotFoundFormat, orderId), []));
        }

        if (order.Status != ShiftStatus.OriginalOrder)
        {
            var reason = order.Status == ShiftStatus.SealedOrder
                ? string.Format(AlreadySealedFormat, order.Name)
                : string.Format(NotAnOrderFormat, order.Name, order.Status);

            return new OrderSealingCheck(null, Refused(orderId, order.Name, reason, []));
        }

        if (groupToAttach.HasValue && HasActiveGroup(order))
        {
            return new OrderSealingCheck(null, Refused(orderId, order.Name, string.Format(GroupAppearedFormat, order.Name), []));
        }

        var missing = CollectMissingRequirements(order)
            .Where(requirement => !groupToAttach.HasValue || requirement != MissingGroupRequirement)
            .ToList();
        if (missing.Count > 0)
        {
            return new OrderSealingCheck(null, Refused(
                orderId,
                order.Name,
                string.Format(MissingRequirementsFormat, order.Name, string.Join(", ", missing)),
                missing));
        }

        return new OrderSealingCheck(order, null);
    }

    public Task<OrderSealingOutcome> SealAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        SealAsync(orderId, groupToAttach: null, cancellationToken);

    public async Task<OrderSealingOutcome> SealAsync(
        Guid orderId, Guid? groupToAttach, CancellationToken cancellationToken = default)
    {
        var check = await CheckAsync(orderId, groupToAttach, cancellationToken);
        if (check.Refusal != null)
        {
            return check.Refusal;
        }

        var order = check.SealableOrder!;
        order.Status = ShiftStatus.SealedOrder;
        if (groupToAttach.HasValue)
        {
            order.GroupItems.Add(new GroupItem
            {
                Id = Guid.NewGuid(),
                ShiftId = order.Id,
                GroupId = groupToAttach.Value,
                ValidFrom = order.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            });
        }

        try
        {
            var plannableShiftId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var sealedResult = await _shiftRepository.PutWithSealedOrderHandling(order);
                if (sealedResult == null)
                {
                    throw new SkillVerificationException(
                        OperationName,
                        string.Format(OrderVanishedFormat, order.Name));
                }

                await _unitOfWork.CompleteAsync();

                await ConfirmPersistedAsync(
                    () => _shiftRepository.GetNoTracking(orderId),
                    persisted => persisted.Status == ShiftStatus.SealedOrder,
                    string.Format(SealedOrderDescriptionFormat, order.Name));

                var newPlannableShiftId = sealedResult.Id;
                await ConfirmPersistedAsync(
                    () => _shiftRepository.GetNoTracking(newPlannableShiftId),
                    persisted => persisted.Status == ShiftStatus.OriginalShift && persisted.OriginalId == orderId,
                    string.Format(PlannableShiftDescriptionFormat, order.Name));

                if (groupToAttach.HasValue)
                {
                    await ConfirmGroupLinkAsync(newPlannableShiftId, groupToAttach.Value, order.Name);
                }

                return newPlannableShiftId;
            });

            return new OrderSealingOutcome(orderId, order.Name, true, plannableShiftId, null, []);
        }
        catch (SkillVerificationException ex)
        {
            return Refused(orderId, order.Name, ex.Message, []);
        }
    }

    private static bool HasActiveGroup(Shift order) => order.GroupItems.Any(gi => !gi.IsDeleted);

    private async Task ConfirmGroupLinkAsync(Guid plannableShiftId, Guid groupId, string orderName)
    {
        var groups = await _shiftRepository.GetGroupsForShift(plannableShiftId);
        if (!groups.Any(group => group.Id == groupId))
        {
            throw new SkillVerificationException(
                OperationName,
                string.Format(VerificationFailedFormat, string.Format(GroupLinkDescriptionFormat, orderName)));
        }
    }

    private static OrderSealingOutcome Refused(
        Guid orderId, string orderName, string reason, IReadOnlyList<string> missing) =>
        new(orderId, orderName, false, null, reason, missing);

    private static async Task ConfirmPersistedAsync(
        Func<Task<Shift?>> reread,
        Func<Shift, bool> isPersisted,
        string description)
    {
        var persisted = await reread();
        if (persisted is null || !isPersisted(persisted))
        {
            throw new SkillVerificationException(
                OperationName,
                string.Format(VerificationFailedFormat, description));
        }
    }
}
