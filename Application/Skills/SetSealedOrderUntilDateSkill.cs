// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that sets the until-date on an already-sealed order (status SealedOrder) or on a container, which has no order
/// and is a family of one row (ContainerShiftRules) — the single
/// permitted change to an otherwise immutable order, per the shift lifecycle documented by
/// explain_shift_lifecycle_order_to_shift. Only allowed once: the sealed order must not already have
/// an until-date. Through the same IShiftFamilyEndService as the edit form it ends the whole family: no work may exist
/// after the requested date on the plannable shift or any split part, those rows get the date too, and a part that starts
/// after it is removed. A shift with an analyse token is answered like a missing one: skills work on the real plan only,
/// and a variant (key) has no skills.
/// </summary>
/// <param name="shiftId">UUID of the sealed order (status SealedOrder) to set the until-date on.</param>
/// <param name="untilDate">ISO date (YYYY-MM-DD) the order ends on; must not be before fromDate.</param>

using System.Globalization;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_sealed_order_until_date")]
public class SetSealedOrderUntilDateSkill : BaseSkillImplementation
{
    private const string UnconfirmedWriteMessage =
        "The until-date was committed, but reading it back did not confirm it. Check the order before trying again.";

    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftFamilyEndService _shiftFamilyEndService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public SetSealedOrderUntilDateSkill(
        IShiftRepository shiftRepository, IShiftFamilyEndService shiftFamilyEndService, IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _shiftRepository = shiftRepository;
        _shiftFamilyEndService = shiftFamilyEndService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var untilDateStr = GetRequiredString(parameters, "untilDate");

        if (!SkillCalendarStringParser.TryParseDateOnly(untilDateStr, context.UserLanguage, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate format: {untilDateStr}. Expected YYYY-MM-DD (e.g. 2026-06-01).");
        }

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null
            || shift.AnalyseToken != null
            || !await _shiftVisibilityGuard.IsVisibleAsync(shift, cancellationToken))
        {
            return SkillResult.Error($"Order with ID {shiftId} not found.");
        }

        if (shift.Status != ShiftStatus.SealedOrder && !ContainerShiftRules.HasOneRowShape(shift))
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
                $"Sealed order '{shift.Name}' already has an until-date ({shift.UntilDate.Value.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}) set. " +
                "This is a one-time change and the field is now locked like every other field on a sealed order.");
        }

        try
        {
            await _shiftFamilyEndService.EndFamilyAsync(shift, untilDate, null, cancellationToken);
        }
        catch (ShiftEndDateRefusedException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var persisted = await _shiftRepository.GetNoTracking(shiftId);
        if (persisted?.UntilDate != untilDate)
        {
            return SkillResult.Error(UnconfirmedWriteMessage);
        }

        var resultData = new
        {
            OrderId = shiftId,
            UntilDate = untilDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            shift.Name,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Until-date {untilDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)} set on sealed order '{shift.Name}' and confirmed in the database " +
            "(verified). This field is now locked. The order's plannable shift and split parts end on the same date; " +
            "any part starting after that date is removed.");
    }
}
