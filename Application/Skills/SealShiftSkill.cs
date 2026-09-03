// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that seals ONE order (Bestellung, Status=OriginalOrder) into a permanently immutable
/// SealedOrder — the one-time lifecycle transition described by explain_shift_lifecycle_order_to_shift.
/// Sealing simultaneously creates the plannable shift (OriginalShift) that get_shift_details,
/// search_shifts and cut_shift then operate on; the sealed order row itself is never changed again
/// afterwards (the only exception being set_sealed_order_until_date). The transition itself lives in
/// IOrderSealingService, which seal_open_orders uses for its batch too.
/// </summary>
/// <param name="shiftId">UUID of the order (status OriginalOrder) to seal.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("seal_shift")]
public class SealShiftSkill : BaseSkillImplementation
{
    private readonly IOrderSealingService _orderSealingService;

    public SealShiftSkill(IOrderSealingService orderSealingService)
    {
        _orderSealingService = orderSealingService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");

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
