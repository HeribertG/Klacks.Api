// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The cluster lifecycle expressed as data, so the collector, the background sweep and the admin REST
/// layer cannot drift apart on which transition is legal. Every transition the learning loop performs
/// goes through <see cref="IsLegalTransition"/>; anything not listed is a programming error rather than
/// a runtime outcome.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningStateMachine
{
    /// <summary>
    /// Statuses a cluster can never leave again. Retention soft-deletes rows in one of these; the
    /// collector must never re-open one, otherwise a wish an admin dismissed would come back on the
    /// next occurrence of the same utterance.
    /// </summary>
    public static readonly IReadOnlyList<string> TerminalStatuses =
    [
        SkillLearningClusterStatuses.Retired,
        SkillLearningClusterStatuses.Dismissed
    ];

    /// <summary>
    /// Statuses retention may soft-delete once they aged past the retention window. Wider than
    /// <see cref="TerminalStatuses"/> on purpose: an unfulfillable cluster is finished business for the
    /// admin card, but it is not terminal in the state machine - a later round may still pick it up, and
    /// it keeps counting recurrences, which is the negative fitness signal stage G3 measures. Making it
    /// terminal instead would have to take both of those away.
    /// </summary>
    public static readonly IReadOnlyList<string> RetentionEligibleStatuses =
    [
        SkillLearningClusterStatuses.Retired,
        SkillLearningClusterStatuses.Dismissed,
        SkillLearningClusterStatuses.Unfulfillable
    ];

    /// <summary>
    /// Statuses whose occurrence counters still accumulate when the same utterance is seen again.
    /// A cluster that already produced an artefact keeps counting too, because a recurrence after
    /// activation is exactly the negative fitness signal stage G3 measures.
    /// </summary>
    public static readonly IReadOnlyList<string> CountingStatuses =
    [
        SkillLearningClusterStatuses.Collecting,
        SkillLearningClusterStatuses.Ready,
        SkillLearningClusterStatuses.Learning,
        SkillLearningClusterStatuses.LearnedPhrase,
        SkillLearningClusterStatuses.LearnedCapability,
        SkillLearningClusterStatuses.Unfulfillable
    ];

    /// <summary>
    /// Legal target statuses per source status. A failed learning round returns to Ready so the next
    /// round can retry with the recorded error text; only after the attempt cap does a cluster become
    /// Unfulfillable. Dismissed is reachable from every non-terminal status because an admin may
    /// discard a wish at any point.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [SkillLearningClusterStatuses.Collecting] =
            [
                SkillLearningClusterStatuses.Ready,
                SkillLearningClusterStatuses.Dismissed
            ],
            [SkillLearningClusterStatuses.Ready] =
            [
                SkillLearningClusterStatuses.Learning,
                SkillLearningClusterStatuses.Dismissed
            ],
            [SkillLearningClusterStatuses.Learning] =
            [
                SkillLearningClusterStatuses.LearnedPhrase,
                SkillLearningClusterStatuses.LearnedCapability,
                SkillLearningClusterStatuses.Unfulfillable,
                SkillLearningClusterStatuses.Ready,
                SkillLearningClusterStatuses.Dismissed
            ],
            [SkillLearningClusterStatuses.LearnedPhrase] =
            [
                SkillLearningClusterStatuses.Retired,
                SkillLearningClusterStatuses.Dismissed
            ],
            [SkillLearningClusterStatuses.LearnedCapability] =
            [
                SkillLearningClusterStatuses.Retired,
                SkillLearningClusterStatuses.Dismissed
            ],
            [SkillLearningClusterStatuses.Unfulfillable] =
            [
                SkillLearningClusterStatuses.Ready,
                SkillLearningClusterStatuses.Retired,
                SkillLearningClusterStatuses.Dismissed
            ],
            [SkillLearningClusterStatuses.Retired] = [],
            [SkillLearningClusterStatuses.Dismissed] = []
        };

    public static bool IsTerminal(string status) => TerminalStatuses.Contains(status, StringComparer.Ordinal);

    public static bool IsRetentionEligible(string status) =>
        RetentionEligibleStatuses.Contains(status, StringComparer.Ordinal);

    public static bool IsCounting(string status) => CountingStatuses.Contains(status, StringComparer.Ordinal);

    public static bool IsLegalTransition(string fromStatus, string toStatus) =>
        AllowedTransitions.TryGetValue(fromStatus, out var allowed)
        && allowed.Contains(toStatus, StringComparer.Ordinal);
}
