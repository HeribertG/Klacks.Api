// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITurnConfirmationScope
{
    void MarkIssuedForSensitiveSkill(string token);

    bool WasIssuedThisTurnForSensitiveSkill(string token);

    /// <summary>
    /// Records a confirmation token this turn issued, whatever the risk class of the skill it holds. A turn
    /// the user stopped drops exactly these tokens again, because the question they belong to was never asked.
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
