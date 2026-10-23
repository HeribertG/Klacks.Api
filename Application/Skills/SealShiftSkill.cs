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
/// anything, and a binding made for another order is refused with the binding mismatch text.
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

    private readonly IOrderSealingService _orderSealingService;

    public SealShiftSkill(IOrderSealingService orderSealingService)
    {
        _orderSealingService = orderSealingService;
    }

    public static ConfirmationBinding BindingFor(Guid orderId) =>
        ConfirmationBinding.Of(ConfirmationBindingKinds.OrderSeal, [orderId.ToString(GuidFormat)]);

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
        var current = BindingFor(shiftId);
        if (!confirmed.Matches(current))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, current));
        }

        var outcome = await _orderSealingService.SealAsync(shiftId, cancellationToken);
        if (!outcome.IsSealed)
        {
            return SkillResult.Error(outcome.FailureReason!);
        }

        var resultData = new
        {
            OrderId = outcome.OrderId,
            PlannableShiftId = outcome.PlannableShiftId,
            Name = outcome.OrderName,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Order '{outcome.OrderName}' sealed (status SealedOrder) and its plannable shift was created and confirmed in the " +
            $"database (verified). Use shiftId=\"{outcome.PlannableShiftId}\" from now on for booking/cutting — the order itself " +
            "stays immutable.");
    }
}
