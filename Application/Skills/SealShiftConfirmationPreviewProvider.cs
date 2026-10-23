// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of seal_shift: when the autonomy gate holds the call, this provider checks the order with the
/// sealing service's own read-only check (IOrderSealingService.CheckAsync, the same refusals SealAsync gives) and
/// describes it - name, abbreviation, period and times - together with what sealing does and that it can never be undone.
/// It refuses, so the gate issues no token, when the shiftId is missing or the order cannot be sealed. The order id is the
/// preview's binding (ConfirmationBindingKinds.OrderSeal): the confirmed replay seals exactly this order or nothing.
/// It only reads.
/// </summary>
/// <param name="orderSealingService">Loads the order and tells whether it may be sealed right now</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

public class SealShiftConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string TimeFormat = "HH\:mm";
    private const string OpenEnd = "open end";

    private const string InvalidShiftIdError =
        "seal_shift needs shiftId, the UUID of the order (status OriginalOrder) to seal. Nothing was stored.";

    private const string SealNote =
        "Sealing turns this order into an immutable sealed order and creates its plannable shift; it can never be undone. " +
        "Exactly this order is bound to the confirmation.";

    private readonly IOrderSealingService _orderSealingService;

    public SealShiftConfirmationPreviewProvider(IOrderSealingService orderSealingService)
    {
        _orderSealingService = orderSealingService;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, SealShiftSkill.SkillName, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(SkillParameterReader.Read<string>(parameters, SealShiftSkill.ShiftIdParameter), out var orderId))
        {
            return SkillConfirmationPreview.Refuse(InvalidShiftIdError);
        }

        var check = await _orderSealingService.CheckAsync(orderId, cancellationToken);
        if (check.Refusal != null)
        {
            return SkillConfirmationPreview.Refuse(check.Refusal.FailureReason!);
        }

        var order = check.SealableOrder!;
        var until = order.UntilDate?.ToString(DateTimeFormats.IsoDate) ?? OpenEnd;
        var text =
            $"Seal the order '{order.Name}' ({order.Abbreviation}), {order.FromDate.ToString(DateTimeFormats.IsoDate)} to " +
            $"{until}, {order.StartShift.ToString(TimeFormat)}-{order.EndShift.ToString(TimeFormat)}. {SealNote}";
        return SkillConfirmationPreview.Show(text, SealShiftSkill.BindingFor(orderId));
    }
}
