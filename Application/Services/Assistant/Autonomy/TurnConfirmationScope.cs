// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scoped per request (= one chat turn): remembers confirmation tokens that were issued for
/// SENSITIVE skills during the current turn, so neither the gate's direct-token path nor
/// confirm_pending_action can redeem them before the user has actually replied. The token
/// itself stays valid — only same-turn redemption is refused.
/// It also remembers every token and proposal hint the turn issued, sensitive or not, so a turn the
/// user stops can drop exactly those again (ITurnConfirmationDiscarder).
/// </summary>
/// <param name="token">The one-time confirmation token issued by the autonomy gate.</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class TurnConfirmationScope : ITurnConfirmationScope
{
    private readonly HashSet<string> _sensitiveTokensIssuedThisTurn = new(StringComparer.Ordinal);
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

    public void MarkIssuedForSensitiveSkill(string token)
    {
        _sensitiveTokensIssuedThisTurn.Add(token);
    }

    public bool WasIssuedThisTurnForSensitiveSkill(string token)
    {
        return _sensitiveTokensIssuedThisTurn.Contains(token);
    }
}
