// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that seals ONE order (Bestellung, Status=OriginalOrder) into a permanently immutable
/// SealedOrder — the one-time lifecycle transition described by explain_shift_lifecycle_order_to_shift.
/// Sealing simultaneously creates the plannable shift (OriginalShift) that get_shift_details,
/// search_shifts and cut_shift then operate on; the sealed order row itself is never changed again
/// afterwards (the only exception being set_sealed_order_until_date).
/// </summary>
/// <param name="shiftId">UUID of the order (status OriginalOrder) to seal.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("seal_shift")]
public class SealShiftSkill : BaseSkillImplementation
{
    private const string SkillName = "seal_shift";

    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SealShiftSkill(IShiftRepository shiftRepository, IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null)
        {
            return SkillResult.Error($"Order with ID {shiftId} not found.");
        }

        if (shift.Status != ShiftStatus.OriginalOrder)
        {
            return shift.Status == ShiftStatus.SealedOrder
                ? SkillResult.Error(
                    $"Order '{shift.Name}' is already sealed (status SealedOrder) and cannot be sealed again. " +
                    "Use search_shifts or get_shift_details to find its plannable shift.")
                : SkillResult.Error(
                    $"Shift '{shift.Name}' has status {shift.Status} — it is a plannable shift, not an order. " +
                    "Only an order (status OriginalOrder) can be sealed.");
        }

        var missing = CollectMissingSealingRequirements(shift);
        if (missing.Count > 0)
        {
            return SkillResult.Error(
                $"Cannot seal order '{shift.Name}' yet — missing/invalid: {string.Join(", ", missing)}. " +
                "Complete these fields on the order first, then call seal_shift again.");
        }

        shift.Status = ShiftStatus.SealedOrder;

        Guid plannableShiftId;
        try
        {
            plannableShiftId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var sealedResult = await _shiftRepository.PutWithSealedOrderHandling(shift);
                if (sealedResult == null)
                {
                    throw new SkillVerificationException(
                        SkillName,
                        $"Order '{shift.Name}' could not be found while sealing — the write was rolled back.");
                }

                await _unitOfWork.CompleteAsync();

                await ConfirmPersistedAsync(
                    SkillName,
                    () => _shiftRepository.GetNoTracking(shiftId),
                    persisted => persisted.Status == ShiftStatus.SealedOrder,
                    $"the sealing of order '{shift.Name}'");

                var newPlannableShiftId = sealedResult.Id;
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _shiftRepository.GetNoTracking(newPlannableShiftId),
                    persisted => persisted.Status == ShiftStatus.OriginalShift && persisted.OriginalId == shiftId,
                    $"the plannable shift created for sealed order '{shift.Name}'");

                return newPlannableShiftId;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var resultData = new
        {
            OrderId = shiftId,
            PlannableShiftId = plannableShiftId,
            shift.Name,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Order '{shift.Name}' sealed (status SealedOrder) and its plannable shift was created and confirmed in the " +
            $"database (verified). Use shiftId=\"{plannableShiftId}\" from now on for booking/cutting — the order itself " +
            "stays immutable.");
    }

    private static List<string> CollectMissingSealingRequirements(Shift shift)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(shift.Abbreviation))
        {
            missing.Add("abbreviation");
        }

        if (string.IsNullOrWhiteSpace(shift.Name))
        {
            missing.Add("name");
        }

        if (shift.FromDate == default)
        {
            missing.Add("fromDate");
        }

        var hasAnyWeekdaySelected = shift.IsMonday || shift.IsTuesday || shift.IsWednesday
            || shift.IsThursday || shift.IsFriday || shift.IsSaturday || shift.IsSunday || shift.IsHoliday;
        if (!hasAnyWeekdaySelected)
        {
            missing.Add("at least one weekday or holiday flag");
        }

        if (!shift.GroupItems.Any(gi => !gi.IsDeleted))
        {
            missing.Add("at least one group");
        }

        if (shift.Quantity <= 0)
        {
            missing.Add("quantity > 0");
        }

        if (shift.SumEmployees <= 0)
        {
            missing.Add("sumEmployees > 0");
        }

        return missing;
    }
}
