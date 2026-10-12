// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Model-facing error texts for a refused confirmation redemption. They state the cause and tell the model to put the
/// question to the user and stop, so the user is asked instead of the action being executed on the model's own say-so.
/// HeldOverMcp is the confirmation text for a call that arrived over MCP: it carries no token and no redemption step,
/// because a token issued there cannot be redeemed anywhere.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class ConfirmationRedemptionMessages
{
    public const string IssuedThisTurn =
        "This confirmation token was issued in this same turn and cannot be redeemed yet. Nothing was executed. " +
        "Ask the user to explicitly confirm the action and stop; the token stays valid and can only be redeemed " +
        "in a later turn, after the user answered with a clear yes.";

    public const string UserConfirmationMissing =
        "The user's latest message is not an explicit confirmation, so the held action was not executed. " +
        "Nothing was changed and the token stays valid. Ask the user to confirm the action with a clear yes " +
        "and stop; redeem the token only in the turn in which the user has answered.";

    public const string HeldOverMcp =
        "The requested action is stored and NOT executed. A confirmation cannot be given through MCP, because the " +
        "server cannot tell the client agent's confirmation from the user's. Tell the user that nothing was done and " +
        "that they can start the action themselves in the Klacks assistant chat.";

    public static string For(ConfirmationRedemptionVerdict verdict) => verdict switch
    {
        ConfirmationRedemptionVerdict.IssuedThisTurn => IssuedThisTurn,
        ConfirmationRedemptionVerdict.UserConfirmationMissing => UserConfirmationMissing,
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "No refusal message exists for this verdict.")
    };
}
