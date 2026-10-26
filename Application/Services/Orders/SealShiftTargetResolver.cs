// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Works out what seal_shift would seal (owner decision 2026-10-25: the group of an order without one comes from the
/// customer's address). An order the sealing check accepts is sealed as it is. An order the check refuses only for its
/// missing group gets the group IOrderGroupPlacementResolver derives (the rule of assign_orders_to_groups) and is checked
/// again with that group; when no group can be derived, the refusal names the reason and tells the model to ask the user
/// for the group, link it with add_shift_to_group and call seal_shift again. Any other refusal is passed on unchanged.
/// It only reads; preview and confirmed replay both call it, so they derive the same group.
/// </summary>
/// <param name="orderSealingService">The sealing check (and the check with a group to attach)</param>
/// <param name="groupPlacementResolver">Derives the group of an order without one from its customer's address</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Services.Orders;

public sealed class SealShiftTargetResolver : ISealShiftTargetResolver
{
    private const string NoGroupDerivedHint =
        " No group could be derived from the customer's address ({0}). Ask the user which group the order belongs to, " +
        "find that group with list_groups, link it to this order with add_shift_to_group (shiftId = the order id) and " +
        "then call seal_shift again.";

    private readonly IOrderSealingService _orderSealingService;
    private readonly IOrderGroupPlacementResolver _groupPlacementResolver;

    public SealShiftTargetResolver(
        IOrderSealingService orderSealingService, IOrderGroupPlacementResolver groupPlacementResolver)
    {
        _orderSealingService = orderSealingService;
        _groupPlacementResolver = groupPlacementResolver;
    }

    public async Task<SealShiftTarget> ResolveAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var check = await _orderSealingService.CheckAsync(orderId, cancellationToken);
        if (check.Refusal == null)
        {
            return new SealShiftTarget(check.SealableOrder, null, null);
        }

        if (!check.Refusal.MissingRequirements.Contains(OrderSealingService.MissingGroupRequirement))
        {
            return new SealShiftTarget(null, null, check.Refusal.FailureReason);
        }

        var placement = await _groupPlacementResolver.ResolveAsync(orderId, cancellationToken);
        if (placement.Assignment == null)
        {
            return new SealShiftTarget(
                null, null,
                check.Refusal.FailureReason
                    + string.Format(CultureInfo.InvariantCulture, NoGroupDerivedHint, placement.UnplacedReason));
        }

        var withGroup = await _orderSealingService.CheckAsync(orderId, placement.Assignment.GroupId, cancellationToken);
        return withGroup.Refusal == null
            ? new SealShiftTarget(withGroup.SealableOrder, placement.Assignment, null)
            : new SealShiftTarget(null, null, withGroup.Refusal.FailureReason);
    }
}
