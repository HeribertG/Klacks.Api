// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that sets the until-date on an already-sealed order (status SealedOrder) — the single
/// permitted change to an otherwise immutable order, per the shift lifecycle documented by
/// explain_shift_lifecycle_order_to_shift. Only allowed once: the sealed order must not already have
/// an until-date, and no work may exist after the requested date anywhere in the order's shift tree
/// (its plannable shift and any split parts). This mirrors the plain save the edit-shift form performs
/// on the sealed order row; the derived plannable shift/splits keep their own until-date unchanged,
/// since they are never resynced from the order after the initial sealing clone.
/// </summary>
/// <param name="shiftId">UUID of the sealed order (status SealedOrder) to set the until-date on.</param>
/// <param name="untilDate">ISO date (YYYY-MM-DD) the order ends on; must not be before fromDate.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_sealed_order_until_date")]
public class SetSealedOrderUntilDateSkill : BaseSkillImplementation
{
    private const string SkillName = "set_sealed_order_until_date";

    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetSealedOrderUntilDateSkill(IShiftRepository shiftRepository, IUnitOfWork unitOfWork)
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
        var untilDateStr = GetRequiredString(parameters, "untilDate");

        if (!DateOnly.TryParse(untilDateStr, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate format: {untilDateStr}. Expected YYYY-MM-DD (e.g. 2026-06-01).");
        }

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null)
        {
            return SkillResult.Error($"Order with ID {shiftId} not found.");
        }

        if (shift.Status != ShiftStatus.SealedOrder)
        {
            if (shift.Status == ShiftStatus.OriginalOrder)
            {
                return SkillResult.Error(
                    $"Order '{shift.Name}' is not sealed yet (status OriginalOrder). Seal it first with seal_shift, " +
                    "then set the until-date.");
            }

            var hint = shift.OriginalId.HasValue
                ? $" Call this skill with the sealed order's ID ({shift.OriginalId}) instead."
                : string.Empty;
            return SkillResult.Error(
                $"Shift '{shift.Name}' has status {shift.Status} — it is a plannable shift derived from the order, " +
                $"not the sealed order itself.{hint}");
        }

        if (shift.UntilDate.HasValue)
        {
            return SkillResult.Error(
                $"Sealed order '{shift.Name}' already has an until-date ({shift.UntilDate:yyyy-MM-dd}) set. " +
                "This is a one-time change and the field is now locked like every other field on a sealed order.");
        }

        if (untilDate < shift.FromDate)
        {
            return SkillResult.Error(
                $"untilDate ({untilDate:yyyy-MM-dd}) must not be before fromDate ({shift.FromDate:yyyy-MM-dd}).");
        }

        var hasFutureWorks = await _shiftRepository.HasWorksAfterDateForOrderTreeAsync(shiftId, untilDate, cancellationToken);
        if (hasFutureWorks)
        {
            return SkillResult.Error(
                $"Cannot set the until-date to {untilDate:yyyy-MM-dd}: work is already planned after that date on " +
                $"order '{shift.Name}' (on its plannable shift or one of its split parts). Move or cancel those " +
                "bookings first, or choose a later until-date.");
        }

        shift.UntilDate = untilDate;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var updated = await _shiftRepository.PutWithSealedOrderHandling(shift);
                if (updated == null)
                {
                    throw new SkillVerificationException(
                        SkillName,
                        $"Order '{shift.Name}' could not be found while setting the until-date — the write was rolled back.");
                }

                await _unitOfWork.CompleteAsync();

                await ConfirmPersistedAsync(
                    SkillName,
                    () => _shiftRepository.GetNoTracking(shiftId),
                    persisted => persisted.UntilDate == untilDate,
                    $"the until-date on sealed order '{shift.Name}'");

                return shiftId;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var resultData = new
        {
            OrderId = shiftId,
            UntilDate = untilDate.ToString("yyyy-MM-dd"),
            shift.Name,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Until-date {untilDate:yyyy-MM-dd} set on sealed order '{shift.Name}' and confirmed in the database " +
            "(verified). This field is now locked. Note: the order's plannable shift and any split parts keep their " +
            "own until-date unchanged — they are not automatically resynced from the order.");
    }
}
