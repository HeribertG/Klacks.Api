// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of seal_shift: when the autonomy gate holds the call, this provider works out what would be sealed
/// with the very resolver the confirmed call uses (ISealShiftTargetResolver: the sealing service's read-only check, and for
/// an order without a group the group derived from the customer's address) and describes it - name, abbreviation, period
/// and times, the derived group and why it was chosen - together with what sealing does and that it can never be undone.
/// It refuses, so the gate issues no token, when the shiftId is missing or the order cannot be sealed; for an order whose
/// group cannot be derived the refusal tells the model to ask the user for the group. The order id, and the derived group
/// when there is one, are the preview's binding (ConfirmationBindingKinds.OrderSeal): the confirmed replay seals exactly
/// this order into exactly this group or nothing. It only reads.
/// </summary>
/// <param name="targetResolver">Works out the order and the group it would be placed in</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

public class SealShiftConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string OpenEnd = "open end";

    private const string InvalidShiftIdError =
        "seal_shift needs shiftId, the UUID of the order (status OriginalOrder) to seal. Nothing was stored.";

    private const string SealNote =
        "Sealing turns this order into an immutable sealed order and creates its plannable shift; it can never be undone. " +
        "Exactly this order is bound to the confirmation.";

    private const string DerivedGroupNote =
        " The order has no group yet: sealing places it in group '{0}' ({1}), derived from the customer's address; a " +
        "different group derived before the confirmation voids it.";

    private readonly ISealShiftTargetResolver _targetResolver;

    public SealShiftConfirmationPreviewProvider(ISealShiftTargetResolver targetResolver)
    {
        _targetResolver = targetResolver;
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

        var target = await _targetResolver.ResolveAsync(orderId, cancellationToken);
        if (target.Refusal != null)
        {
            return SkillConfirmationPreview.Refuse(target.Refusal);
        }

        var order = target.Order!;
        var from = order.FromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
        var until = order.UntilDate?.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture) ?? OpenEnd;
        var start = order.StartShift.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
        var end = order.EndShift.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
        var groupNote = target.DerivedGroup == null
            ? string.Empty
            : string.Format(
                CultureInfo.InvariantCulture, DerivedGroupNote, target.DerivedGroup.GroupName, target.DerivedGroup.MatchReason);
        var text = $"Seal the order '{order.Name}' ({order.Abbreviation}), {from} to {until}, {start}-{end}.{groupNote} {SealNote}";
        return SkillConfirmationPreview.Show(text, SealShiftSkill.BindingFor(orderId, target.DerivedGroup?.GroupId));
    }
}
