// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scoped per request (= one chat turn): remembers every confirmation token and proposal hint the current turn issued,
/// whatever the risk class of the skill they belong to. Neither the gate's direct-token path nor confirm_pending_action
/// can redeem a token before the user has actually replied (IConfirmationRedemptionGuard reads WasIssuedThisTurn); the
/// token itself stays valid, only same-turn redemption is refused. A turn the user stops drops exactly the remembered
/// tokens and hints again (ITurnConfirmationDiscarder).
/// </summary>
/// <param name="token">The one-time confirmation token issued by the autonomy gate.</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class TurnConfirmationScope : ITurnConfirmationScope
{
    private readonly HashSet<string> _issuedTokens = new(StringComparer.Ordinal);
    private readonly HashSet<string> _proposalHintSkills = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> IssuedTokens => _issuedTokens;

    public IReadOnlyCollection<string> ProposalHintSkills => _proposalHintSkills;

    public void MarkIssued(string token)
    {
        _issuedTokens.Add(token);
    }

    public void MarkProposalHint(string applySkillName)
    {
        _proposalHintSkills.Add(applySkillName);
    }

    public bool WasIssuedThisTurn(string token)
    {
        return _issuedTokens.Contains(token);
    }
}
