// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a learning cluster is evidence of a recipe trigger that matches too broadly, and
/// spells the two sentences such a verdict produces. Read off the cluster's own signal histogram rather
/// than off its cases, so the decision costs no extra query: the histogram is recomputed on every
/// occurrence and is therefore always current. One explicit human judgement in the histogram is enough to
/// veto the verdict, because a verdict dismisses the cluster and that judgement would go unread.
/// </summary>
/// <param name="signalKindsJson">Counter per signal kind as stored on the cluster, e.g. {"recipe_declined":4}</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class RecipeDeclineClusterPolicy
{
    /// <summary>
    /// Sentence recorded on the proposal. Argument 0 is the recipe name, argument 1 the occurrence count.
    /// </summary>
    public const string JustificationTemplate =
        "The recipe '{0}' was declined at its confirmation question for this utterance {1} time(s). "
        + "Its trigger matches an intent the user does not have; narrow the trigger instead of routing it.";

    /// <summary>
    /// Sentence recorded on the cluster when it is closed. Argument 0 is the recipe name.
    /// </summary>
    public const string DismissalTemplate =
        "Trigger too broad: a narrowing proposal for the recipe '{0}' was opened for review; "
        + "nothing was learned for this utterance.";

    // Where somebody stated something about the routing - named the wrong skill, said none was needed, or
    // marked the turn as not helpful - the cluster carries a verdict a narrowing proposal cannot express,
    // and a single such case outranks any number of declines. Refusals and inferred corrections carry no
    // such statement and keep ranking by count, ties included.
    private static readonly HashSet<string> ExplicitHumanSignals = new(StringComparer.Ordinal)
    {
        SkillLearningSignals.WrongSkill,
        SkillLearningSignals.NoneNeeded,
        SkillLearningSignals.Explicit,
    };

    /// <summary>
    /// The recipe the declines are about: the skill name of the newest case whose signal is recipe_declined,
    /// or null when no such case names one. Read off those cases alone and never off the cluster as a whole,
    /// because a mixed cluster also holds cases of other signals whose chosen skill is an ordinary routing
    /// target - naming one of those would ask an administrator to narrow a trigger that never fired.
    /// </summary>
    /// <param name="cases">Cases of the cluster, newest first</param>
    public static string? ResolveDeclinedRecipeName(IReadOnlyList<SkillLearningCase> cases)
    {
        foreach (var learningCase in cases)
        {
            if (string.Equals(
                    learningCase.Signal, SkillLearningSignals.RecipeDeclined, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(learningCase.ChosenSkill))
            {
                return learningCase.ChosenSkill;
            }
        }

        return null;
    }

    public static bool IsTriggerTooBroad(string? signalKindsJson)
    {
        var histogram = Parse(signalKindsJson);
        if (histogram == null
            || !histogram.TryGetValue(SkillLearningSignals.RecipeDeclined, out var declines)
            || declines <= 0)
        {
            return false;
        }

        foreach (var entry in histogram)
        {
            if (string.Equals(entry.Key, SkillLearningSignals.RecipeDeclined, StringComparison.Ordinal)
                || entry.Value <= 0)
            {
                continue;
            }

            if (ExplicitHumanSignals.Contains(entry.Key) || entry.Value > declines)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, int>? Parse(string? signalKindsJson)
    {
        if (string.IsNullOrWhiteSpace(signalKindsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(signalKindsJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
