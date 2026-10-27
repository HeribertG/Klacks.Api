// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Labels the outcome of one scored goldset item with the reason it hit or missed, so the numbers of a run separate
/// measurement artifacts (a skill the environment does not have, a lookup the model rightly made first) from real
/// selection errors. A pure label: it reads the verdicts the scorer already wrote and changes none of them.
/// Rules apply in priority order, for items that expect a tool and were neither excluded, errored, a recipe item nor a
/// rights-refusal item: expected skill not installed, expected skill not in the toolset (cut by the cap or never
/// retrieved), correct first choice, reached through the follow-up, no tool chosen, a surface or navigation skill
/// chosen for a mutation, a lookup chosen where the mutation needs an id, a wrong write, a wrong read.
/// </summary>
/// <param name="item">The goldset item that was replayed</param>
/// <param name="result">The scorer's verdicts for the item</param>
/// <param name="replay">The replay outcome, supplying the toolset trace and the effects of the offered skills</param>
/// <param name="catalog">The installed skills, or null when unknown, which skips the not-registered rule</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnEvalMissClassifier
{
    private static readonly HashSet<string> SurfaceSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        SkillNames.SearchInList,
        SkillNames.SearchAndNavigate,
        SkillNames.GetPageControls,
        SkillNames.GetUserContext,
        SkillNames.NavigateTo
    };

    public static EvalMissKind Classify(
        TurnGoldsetItem item,
        TurnEvalItemResult result,
        TurnReplayResult replay,
        TurnEvalSkillCatalog? catalog)
    {
        if (item.ExpectedTool == null
            || result.Excluded
            || result.Errored
            || item.ExpectedRecipe != null
            || item.ExpectsRightsRefusal)
        {
            return EvalMissKind.NotApplicable;
        }

        var acceptable = AcceptableTools(item);

        if (catalog != null && !acceptable.Any(name => catalog.TryGet(name, out _)))
        {
            return EvalMissKind.ToolNotRegistered;
        }

        if (result.RetrievalHit != true)
        {
            return WasTruncated(replay, acceptable)
                ? EvalMissKind.NotInToolsetTruncated
                : EvalMissKind.NotInToolsetNotRetrieved;
        }

        if (result.SelectionHit == true)
        {
            return EvalMissKind.Correct;
        }

        if (result.ReachedHit == true)
        {
            return EvalMissKind.ReachedViaFollowUp;
        }

        if (result.ChosenTool == null)
        {
            return EvalMissKind.NoToolText;
        }

        return ClassifyWrongChoice(item, result, replay, catalog, acceptable);
    }

    private static EvalMissKind ClassifyWrongChoice(
        TurnGoldsetItem item,
        TurnEvalItemResult result,
        TurnReplayResult replay,
        TurnEvalSkillCatalog? catalog,
        List<string> acceptable)
    {
        var expectedInfo = FindInfo(acceptable, catalog);
        var expectsMutation = expectedInfo?.Effect == SkillEffect.Mutate
            || (expectedInfo == null && ReplayEffect(replay, item.ExpectedTool) == SkillEffect.Mutate);
        var chosenEffect = ChosenEffect(result.ChosenTool, replay, catalog);
        var chosenIsSurface = SurfaceSkills.Contains(result.ChosenTool!);

        if (expectsMutation && chosenIsSurface)
        {
            return EvalMissKind.SurfaceSearch;
        }

        if (expectsMutation
            && expectedInfo is { HasRequiredIdParameter: true }
            && !chosenIsSurface
            && chosenEffect is SkillEffect.Read or SkillEffect.Advise or SkillEffect.Explain)
        {
            return EvalMissKind.LookupFirst;
        }

        return chosenEffect == SkillEffect.Mutate ? EvalMissKind.WrongWrite : EvalMissKind.WrongRead;
    }

    private static List<string> AcceptableTools(TurnGoldsetItem item)
    {
        var tools = new List<string> { item.ExpectedTool! };
        tools.AddRange(item.AlternativeTools);
        return tools;
    }

    private static bool WasTruncated(TurnReplayResult replay, List<string> acceptable) =>
        replay.ToolsetTrace != null
        && replay.ToolsetTrace.TruncatedNames.Any(
            name => acceptable.Any(tool => string.Equals(tool, name, StringComparison.OrdinalIgnoreCase)));

    private static TurnEvalSkillInfo? FindInfo(List<string> acceptable, TurnEvalSkillCatalog? catalog)
    {
        if (catalog == null)
        {
            return null;
        }

        foreach (var tool in acceptable)
        {
            if (catalog.TryGet(tool, out var info))
            {
                return info;
            }
        }

        return null;
    }

    private static SkillEffect? ChosenEffect(string? chosenTool, TurnReplayResult replay, TurnEvalSkillCatalog? catalog)
    {
        if (catalog != null && catalog.TryGet(chosenTool, out var info))
        {
            return info.Effect;
        }

        return ReplayEffect(replay, chosenTool);
    }

    private static SkillEffect? ReplayEffect(TurnReplayResult replay, string? tool) =>
        tool != null && replay.ToolEffects.TryGetValue(tool, out var effect) ? effect : null;
}
