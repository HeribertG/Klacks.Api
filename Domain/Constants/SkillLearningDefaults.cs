// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fallback values for the learning loop, used whenever the matching settings key is absent or unparsable.
/// The thresholds start deliberately low because the loop only learns once people actually talk to the
/// assistant; they are settings-backed so they can be raised with real traffic without a deploy.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningDefaults
{
    public const int MinOccurrences = 3;
    public const int MinDistinctUsers = 2;
    public const int PruneDays = 30;
    public const int RetentionDays = 90;
    public const bool ReportOptIn = false;

    /// <summary>
    /// Maximum length of the stored excerpt of a user utterance. Nothing longer than this is ever
    /// persisted by the learning loop - the full message never leaves the turn.
    /// </summary>
    public const int ExcerptMaxLength = 120;

    /// <summary>
    /// Shortest utterance that may open a case at all. Below this the refusal phrases match noise
    /// ("nein", "was?") far more often than a real capability wish.
    /// </summary>
    public const int MinTokenCount = 3;

    public const int ToolsetCandidatesMax = 30;

    public const int MaxLearningAttempts = 2;

    /// <summary>
    /// How long a claimed cluster may stay in Learning before another instance may take it over, so a
    /// process that died mid-round does not park the cluster forever.
    /// </summary>
    public const int StaleClaimMinutes = 60;
}
