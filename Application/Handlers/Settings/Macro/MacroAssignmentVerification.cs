// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Checks shared by the macro switch and the undo handler, run inside their transaction. Before a holder is written, a
/// fresh untracked read must still show the holder with the reference the plan started from (the plan is made before the
/// transaction; a holder that vanished or changed in between aborts the whole switch before anything is committed). After
/// the write the holder row must have existed, and after the commit a fresh untracked read of every written holder must
/// show the intended reference; otherwise an InvalidRequestException rolls the whole transaction back instead of
/// reporting a switch that did not persist.
/// </summary>
/// <param name="references">Reads the holders untracked, straight from the database</param>
/// <param name="change">A planned reference change: the holder as planned, the reference before and after</param>
/// <param name="written">Whether the reference could be written (false when the holder vanished)</param>
/// <param name="holder">The holder that was written</param>
/// <param name="changes">The references that must now be stored</param>

using System.Globalization;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Services.Macros;

namespace Klacks.Api.Application.Handlers.Settings.Macro;

internal static class MacroAssignmentVerification
{
    private const string HolderVanishedMessage = "'{0}' no longer exists; nothing was changed.";
    private const string HolderChangedMessage =
        "The macro of '{0}' changed while the macro switch was being written; nothing was changed. Check the current "
        + "state and ask again.";
    private const string NotConfirmedMessage =
        "The macro switch on '{0}' could not be confirmed in the database after the write; the whole switch was rolled "
        + "back.";

    internal static async Task EnsureUnchangedSincePlanAsync(
        IMacroReferenceRepository references, MacroReferenceChange change, CancellationToken cancellationToken)
    {
        var current = await references.FindHolderAsync(change.Holder.Target, change.Holder.Id, cancellationToken);
        if (current == null)
        {
            throw new InvalidRequestException(Format(HolderVanishedMessage, change.Holder.Name));
        }

        if (current.MacroId != change.FromMacroId)
        {
            throw new InvalidRequestException(Format(HolderChangedMessage, change.Holder.Name));
        }
    }

    internal static void EnsureHolderStillExists(bool written, MacroReferenceHolder holder)
    {
        if (!written)
        {
            throw new InvalidRequestException(Format(HolderVanishedMessage, holder.Name));
        }
    }

    internal static async Task VerifyAsync(
        IMacroReferenceRepository references,
        IReadOnlyList<MacroReferenceChange> changes,
        CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var persisted = await references.FindHolderAsync(change.Holder.Target, change.Holder.Id, cancellationToken);
            if (persisted == null || persisted.MacroId != change.ToMacroId)
            {
                throw new InvalidRequestException(Format(NotConfirmedMessage, change.Holder.Name));
            }
        }
    }

    private static string Format(string format, string name) =>
        string.Format(CultureInfo.InvariantCulture, format, MacroAssignmentNames.Safe(name));
}
