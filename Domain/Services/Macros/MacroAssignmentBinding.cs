// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one place that turns a macro switch plan or an undo plan into the facts a confirmation is bound to: one item per
/// macro reference that would be written, as target|holder id|macro now|macro afterwards (ids in "N" format, "-" for no
/// macro; Guid.Empty counts as none, as in production). The preview provider stores the result with the token, and the
/// handler that plans the confirmed call compares it with its own fresh plan right before it writes, so both sides always
/// form the items the same way. Bound is what is written, not what is estimated: the entry counts, the warnings, the
/// samples, the holder names and the members that are only counted because they already use the target macro are shown
/// to the user but do not flow in.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Services.Macros;

public static class MacroAssignmentBinding
{
    private const string NoMacro = "-";
    private const string ItemFormat = "{0}|{1:N}|{2}|{3}";
    private const string MacroIdFormat = "N";

    public static ConfirmationBinding Of(MacroAssignmentPlan plan) =>
        ConfirmationBinding.Of(ConfirmationBindingKinds.MacroSwitch, plan.Changes.Select(Describe));

    public static ConfirmationBinding Of(MacroRevertPlan plan) =>
        ConfirmationBinding.Of(ConfirmationBindingKinds.MacroUndo, plan.Changes.Select(Describe));

    private static string Describe(MacroReferenceChange change) =>
        string.Format(
            CultureInfo.InvariantCulture,
            ItemFormat,
            change.Holder.Target,
            change.Holder.Id,
            Format(MacroAssignmentPolicy.AsReference(change.FromMacroId)),
            Format(MacroAssignmentPolicy.AsReference(change.ToMacroId)));

    private static string Format(Guid? macroId) => macroId?.ToString(MacroIdFormat) ?? NoMacro;
}
