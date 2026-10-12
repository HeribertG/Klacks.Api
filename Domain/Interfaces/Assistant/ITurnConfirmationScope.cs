// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITurnConfirmationScope
{
    /// <summary>
    /// True when this turn issued the token, whatever the risk class of the skill it holds. Neither the gate's
    /// direct-token path nor confirm_pending_action may redeem such a token before the user has actually replied.
    /// </summary>
    /// <param name="token">The one-time token to look up</param>
    bool WasIssuedThisTurn(string token);

    /// <summary>
    /// Records a confirmation token this turn issued, whatever the risk class of the skill it holds. The token
    /// cannot be redeemed in this turn, and a turn the user stops drops exactly these tokens again, because the
    /// question they belong to was never asked.
    /// </summary>
    /// <param name="token">The one-time token the pending-confirmation store issued</param>
    void MarkIssued(string token);

    /// <summary>
    /// Records that this turn left a proposal hint for the given apply skill.
    /// </summary>
    /// <param name="applySkillName">The apply skill the hint offers next turn</param>
    void MarkProposalHint(string applySkillName);

    IReadOnlyCollection<string> IssuedTokens { get; }

    IReadOnlyCollection<string> ProposalHintSkills { get; }
}
