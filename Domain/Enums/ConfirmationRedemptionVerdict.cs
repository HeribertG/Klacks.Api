// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of asking whether a confirmation token may be redeemed right now: Allowed, refused because the token was
/// issued in the same turn, or refused because the call carries no proof that the user replied with a yes.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum ConfirmationRedemptionVerdict
{
    Allowed = 0,
    IssuedThisTurn = 1,
    UserConfirmationMissing = 2
}
