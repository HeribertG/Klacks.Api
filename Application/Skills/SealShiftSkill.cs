// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that seals ONE order (Bestellung, Status=OriginalOrder) into a permanently immutable
/// SealedOrder — the one-time lifecycle transition described by explain_shift_lifecycle_order_to_shift.
/// Sealing simultaneously creates the plannable shift (OriginalShift) that get_shift_details,
/// search_shifts and cut_shift then operate on; the sealed order row itself is never changed again
/// afterwards (the only exception being set_sealed_order_until_date). The transition itself lives in
/// IOrderSealingService, which seal_open_orders uses for its batch too. Because it can never be undone, it runs only as
/// the confirmed replay of its server preview (SealShiftConfirmationPreviewProvider): without a confirmed binding of kind
/// OrderSeal (a plan step, any context that bypasses the autonomy gate) it refuses with BindingRequired before it reads
/// anything, and a binding made for another order is refused with the binding mismatch text before anything is read.
/// An order without a group is placed in the group derived from the customer's address (owner decision 2026-10-25,
/// ISealShiftTargetResolver): that group is part of the binding, so a derivation that changed since the preview, or a
/// group the order got of its own in the meantime, voids the confirmation; otherwise the group is attached and sealed in
/// one transaction.
/// </summary>
/// <param name="shiftId">UUID of the order (status OriginalOrder) to seal.</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class SealShiftSkill : BaseSkillImplementation
{
    public const string SkillName = "seal_shift";

    public const string ShiftIdParameter = "shiftId";

    private const string GuidFormat = "N";

    private const string GroupFactPrefix = "group:";

    private readonly ISealShiftTargetResolver _targetResolver;
    private readonly IOrderSealingService _orderSealingService;

    public SealShiftSkill(ISealShiftTargetResolver targetResolver, IOrderSealingService orderSealingService)
    {
        _targetResolver = targetResolver;
        _orderSealingService = orderSealingService;
    }

    /// <summary>The facts a seal confirmation is bound to: the order, and the derived group when one is attached.</summary>
    /// <param name="orderId">The order to seal</param>
    /// <param name="derivedGroupId">The group derived from the customer's address; null when the order has its own group</param>
    public static ConfirmationBinding BindingFor(Guid orderId, Guid? derivedGroupId = null) =>
        ConfirmationBinding.Of(
            ConfirmationBindingKinds.OrderSeal,
            derivedGroupId.HasValue
                ? [OrderFact(orderId), GroupFactPrefix + derivedGroupId.Value.ToString(GuidFormat)]
                : [OrderFact(orderId)]);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (context.ConfirmedActionBinding is not { Kind: ConfirmationBindingKinds.OrderSeal } confirmed)
        {
            return SkillResult.Error(
                string.Format(CultureInfo.InvariantCulture, ConfirmationBindingMessages.BindingRequired, SkillName));
        }

        var shiftId = GetRequiredGuid(parameters, ShiftIdParameter);
        if (confirmed.Items.Count > 0 && !confirmed.Items.Contains(OrderFact(shiftId), StringComparer.Ordinal))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, BindingFor(shiftId)));
        }

        var target = await _targetResolver.ResolveAsync(shiftId, cancellationToken);
        if (target.Refusal != null)
        {
            return SkillResult.Error(target.Refusal);
        }

        var derivedGroup = target.DerivedGroup;
        var current = BindingFor(shiftId, derivedGroup?.GroupId);
        if (!confirmed.Matches(current))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, current));
        }

        var outcome = await _orderSealingService.SealAsync(shiftId, derivedGroup?.GroupId, cancellationToken);
        if (!outcome.IsSealed)
        {
            return SkillResult.Error(outcome.FailureReason!);
        }

        var resultData = new
        {
            OrderId = outcome.OrderId,
            PlannableShiftId = outcome.PlannableShiftId,
            Name = outcome.OrderName,
            AttachedGroupId = derivedGroup?.GroupId,
            AttachedGroupName = derivedGroup?.GroupName,
            Verified = true
        };

        var groupNote = derivedGroup == null
            ? string.Empty
            : $" It had no group and was placed in group '{derivedGroup.GroupName}' ({derivedGroup.MatchReason}).";

        return SkillResult.SuccessResult(
            resultData,
            $"Order '{outcome.OrderName}' sealed (status SealedOrder) and its plannable shift was created and confirmed in the " +
            $"database (verified).{groupNote} Use shiftId=\"{outcome.PlannableShiftId}\" from now on for booking/cutting — the " +
            "order itself stays immutable.");
    }

    private static string OrderFact(Guid orderId) => orderId.ToString(GuidFormat);
}
