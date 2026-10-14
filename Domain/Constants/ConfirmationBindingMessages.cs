// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Model-facing error texts for a confirmation that is bound to the facts of its preview. They tell the model that nothing
/// was changed and that a fresh preview and a fresh confirmation are needed. Describe picks ListChanged when both sides
/// carry their items (it can count what changed) and ScopeChanged otherwise.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Constants;

public static class ConfirmationBindingMessages
{
    public const string BindingRequired =
        "'{0}' only runs as the confirmed replay of its own server preview. Call it without a token to get the "
        + "preview, show it to the user and ask for confirmation.";

    public const string ListChanged =
        "What this confirmation was given for has changed since the preview: {0} item(s) were shown, {1} apply now "
        + "({2} new, {3} no longer there). Nothing was changed and this confirmation is void. Repeat the call without a "
        + "confirmation token to get a fresh preview, show it to the user and ask for a new confirmation.";

    public const string ScopeChanged =
        "What this confirmation was given for has changed since the preview. Nothing was changed and this "
        + "confirmation is void. Repeat the call without a confirmation token to get a fresh preview, show it to the "
        + "user and ask for a new confirmation.";

    public const string DirectRedemptionRefused =
        "'{0}' shows a server preview before it runs, so its confirmation token can only be redeemed through "
        + "'confirm_pending_action'. Nothing was executed; the token stays valid.";

    public static string Describe(ConfirmationBinding stored, ConfirmationBinding current)
    {
        var diff = stored.DiffTo(current);
        return diff == null
            ? ScopeChanged
            : string.Format(
                CultureInfo.InvariantCulture,
                ListChanged,
                diff.ShownCount,
                diff.CurrentCount,
                diff.AddedCount,
                diff.GoneCount);
    }
}
