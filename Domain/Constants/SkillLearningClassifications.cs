// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What kind of gap a ready cluster represents, as judged once per learning run. PhraseGap means a skill
/// for the wish exists and retrieval simply does not find it, which the loop can fix by itself.
/// Composable means no single skill covers the wish but existing ones could be chained - recorded and
/// left to stage G3. NeedsCode means neither, which ends the cluster as unfulfillable.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningClassifications
{
    public const string PhraseGap = "phrase_gap";
    public const string Composable = "composable";
    public const string NeedsCode = "needs_code";

    public static readonly IReadOnlyList<string> All = [PhraseGap, Composable, NeedsCode];

    public static bool IsKnown(string? value) =>
        value != null && All.Contains(value, StringComparer.Ordinal);
}
