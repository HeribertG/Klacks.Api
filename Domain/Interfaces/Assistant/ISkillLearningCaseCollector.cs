// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningCaseCollector
{
    Task CollectFromTurnAsync(SkillLearningTurn turn, CancellationToken cancellationToken = default);

    Task CollectCorrectionAsync(SkillLearningCorrection correction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts a thumbs-down on a captured turn as an explicit negative case (W1.8). Unlike the correction
    /// path there is no expected skill - the user only said the answer did not help.
    /// </summary>
    Task CollectNotHelpfulFeedbackAsync(SkillLearningFeedback feedback, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the preceding utterance as a case of its own cluster after the following turn negated it.
    /// The cluster key comes from the stored trajectory hash, because the preceding message itself is
    /// never persisted and hashing its excerpt would open a rival cluster for long utterances.
    /// </summary>
    Task CollectImplicitCorrectionAsync(
        SkillLearningImplicitCorrection correction, CancellationToken cancellationToken = default);
}
