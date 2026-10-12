// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of the autonomy gate's direct-token path: the token was consumed and the call may run (Redeemed), it was
/// not usable (neither flag set: absent, unknown, expired or issued for other parameters), or the redemption was
/// refused because the user has not answered yet (RefusalMessage), which the gate turns into a clean skill error.
/// </summary>
/// <param name="Redeemed">True when the token was consumed for exactly this call</param>
/// <param name="RefusalMessage">Model-facing reason when the redemption was refused; null otherwise</param>

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

internal readonly record struct TokenRedemption(bool Redeemed, string? RefusalMessage)
{
    public static TokenRedemption NotRedeemed => new(false, null);

    public static TokenRedemption Consumed => new(true, null);

    public static TokenRedemption Refused(string message) => new(false, message);
}
