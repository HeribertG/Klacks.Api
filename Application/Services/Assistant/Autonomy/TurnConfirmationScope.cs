// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scoped per request (= one chat turn): remembers confirmation tokens that were issued for
/// SENSITIVE skills during the current turn, so neither the gate's direct-token path nor
/// confirm_pending_action can redeem them before the user has actually replied. The token
/// itself stays valid — only same-turn redemption is refused.
/// </summary>
/// <param name="token">The one-time confirmation token issued by the autonomy gate.</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class TurnConfirmationScope : ITurnConfirmationScope
{
    private readonly HashSet<string> _sensitiveTokensIssuedThisTurn = new(StringComparer.Ordinal);

    public void MarkIssuedForSensitiveSkill(string token)
    {
        _sensitiveTokensIssuedThisTurn.Add(token);
    }

    public bool WasIssuedThisTurnForSensitiveSkill(string token)
    {
        return _sensitiveTokensIssuedThisTurn.Contains(token);
    }
}
