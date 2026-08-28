// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Which learning signal a correction type produces, if any. Deliberately a total map over
/// <see cref="CorrectionTypes"/> instead of a membership test against SkillLearningSignals.All: the two
/// constant sets only look alike. All contains refusal, which is never a correction type, and a plain
/// name comparison silently promotes any future signal named like a correction type into the learning
/// loop. A null value means the correction says nothing about which capability was missing - a wrong
/// parameter and a repeated request are about the turn, not about routing.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class CorrectionTypeLearningSignals
{
    public static readonly IReadOnlyDictionary<string, string?> ByCorrectionType =
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [CorrectionTypes.None] = null,
            [CorrectionTypes.WrongParam] = null,
            [CorrectionTypes.RepeatedRequest] = null,
            [CorrectionTypes.WrongSkill] = SkillLearningSignals.WrongSkill,
            [CorrectionTypes.NoneNeeded] = SkillLearningSignals.NoneNeeded,
            [CorrectionTypes.Implicit] = SkillLearningSignals.Implicit
        };

    public static string? Resolve(string? correctionType)
    {
        if (string.IsNullOrWhiteSpace(correctionType))
        {
            return null;
        }

        return ByCorrectionType.TryGetValue(correctionType, out var signal) ? signal : null;
    }
}
