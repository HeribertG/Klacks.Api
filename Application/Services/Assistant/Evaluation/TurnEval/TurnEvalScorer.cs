// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure scoring logic for turn-selection evals: compares the replayed tool choice and
/// arguments against the goldset expectation, aggregates per-item results into
/// dimensions and computes the weighted composite. Entity resolution for
/// resolved-entity-id slots happens outside (async) and is passed in as precomputed
/// verdicts keyed by slot name.
///
/// Scorer version 2 (2026-09-02) is NOT comparable with version 1 runs. Three corrections:
/// (1) an item without any evaluated slot no longer scores a vacuous SlotScore of 1.0 - it
/// contributes nothing to the slot dimension, and the dimension is dropped from the composite
/// when no item measured a slot; (2) latency left the composite entirely (with the previous
/// 8000 ms normaliser it was 0 in nearly every run, so the only visible deltas between
/// iterations were latency noise) - it is still aggregated and persisted, but as a reported
/// figure, not as quality; (3) recipe items (ExpectedRecipe) now form their own weighted
/// dimension instead of entering the composite with weight 0 while still counting towards the
/// pass rate. Compare runs only within the same <see cref="ScorerVersion"/> - see the
/// scorer_version column on eval_runs.
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.Skills;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnEvalScorer
{
    /// <summary>
    /// Version of the scoring rules that produced a composite. Persisted on every EvalRun so runs
    /// scored under different rules are never compared. Bump whenever a weight, a dimension or a
    /// per-item verdict changes.
    /// </summary>
    public const int ScorerVersion = 2;

    /// <summary>Honesty mode demanding a refusal or clarifying question without any invented fact.</summary>
    public const string HonestyModeMustAbstain = "must-abstain";

    private const double ToolWeight = 0.45;
    private const double SlotWeight = 0.20;
    private const double NoToolWeight = 0.10;
    private const double RecipeWeight = 0.10;
    private const double HonestyWeight = 0.15;

    public static TurnEvalItemResult ScoreItem(
        TurnGoldsetItem item,
        TurnReplayResult replay,
        IReadOnlyDictionary<string, bool>? resolvedNameSlots = null)
    {
        var result = new TurnEvalItemResult
        {
            ItemId = item.Id,
            ExpectedTool = item.ExpectedTool,
            ExpectedRecipe = item.ExpectedRecipe,
            ChosenTool = replay.ChosenTool,
            RecipeWouldForce = replay.RecipeWouldForce,
            EngineRecipeWouldTrigger = replay.EngineRecipeWouldTrigger,
            Excluded = replay.RecipeWouldForce || replay.EngineRecipeWouldTrigger,
            ExpectedToolAvailable = ComputeExpectedToolAvailable(item, replay),
            Errored = !replay.Success,
            Error = replay.Error,
            LatencyMs = replay.LatencyMs,
            Cost = replay.Cost
        };

        // W0.5: recipe items measure "did the expected recipe engage" instead of being excluded the
        // moment any recipe hijacks the turn. A different recipe engaging is a miss, not an exclusion.
        if (item.ExpectedRecipe != null)
        {
            result.Excluded = false;
            result.RecipeHit = replay.Success
                && (string.Equals(replay.ForcedRecipeName, item.ExpectedRecipe, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(replay.TriggeredRecipeName, item.ExpectedRecipe, StringComparison.OrdinalIgnoreCase));
            result.Passed = result.RecipeHit == true;
            return result;
        }

        if (item.ExpectedTool == null)
        {
            result.NoToolCorrect = replay.Success && replay.ChosenTool == null;

            if (item.Honesty != null
                && string.Equals(item.Honesty.Mode, HonestyModeMustAbstain, StringComparison.OrdinalIgnoreCase))
            {
                ScoreHonesty(item, replay, result);
                result.Passed = !result.Excluded && result.NoToolCorrect == true && result.HonestyCorrect == true;
                return result;
            }

            result.Passed = !result.Excluded && result.NoToolCorrect == true;
            return result;
        }

        var toolHit = replay.Success
            && replay.ChosenTool != null
            && (string.Equals(replay.ChosenTool, item.ExpectedTool, StringComparison.OrdinalIgnoreCase)
                || item.AlternativeTools.Any(t => string.Equals(replay.ChosenTool, t, StringComparison.OrdinalIgnoreCase)));
        result.ToolHit = toolHit;

        if (toolHit)
        {
            result.SlotScore = ScoreSlots(item, replay, resolvedNameSlots, result);
        }

        result.Passed = !result.Excluded && toolHit && (result.SlotScore ?? 1.0) >= 1.0;
        return result;
    }

    public static TurnEvalDimensions Aggregate(IReadOnlyList<TurnEvalItemResult> items)
    {
        var active = items.Where(i => !i.Excluded).ToList();
        var toolItems = active.Where(i => i.ExpectedTool != null).ToList();
        var noToolItems = active.Where(i => i.ExpectedTool == null && i.ExpectedRecipe == null).ToList();
        var recipeItems = active.Where(i => i.ExpectedRecipe != null).ToList();
        var slotItems = toolItems.Where(i => i.ToolHit == true && i.SlotScore != null).ToList();
        var measuredLatency = active.Where(i => !i.Errored).ToList();

        var nameSlotsEvaluated = active.Sum(i => i.NameSlotsEvaluated);
        var nameSlotsResolved = active.Sum(i => i.NameSlotsResolved);
        var honestyItems = active.Where(i => i.HonestyCorrect != null).ToList();

        return new TurnEvalDimensions(
            HonestyAccuracy: honestyItems.Count == 0 ? null : honestyItems.Average(i => i.HonestyCorrect == true ? 1.0 : 0.0),
            ToolAccuracy: toolItems.Count == 0 ? null : toolItems.Average(i => i.ToolHit == true ? 1.0 : 0.0),
            SlotAccuracy: slotItems.Count == 0 ? null : slotItems.Average(i => i.SlotScore!.Value),
            NoToolAccuracy: noToolItems.Count == 0 ? null : noToolItems.Average(i => i.NoToolCorrect == true ? 1.0 : 0.0),
            RecipeAccuracy: recipeItems.Count == 0 ? null : recipeItems.Average(i => i.RecipeHit == true ? 1.0 : 0.0),
            NameResolutionAccuracy: nameSlotsEvaluated == 0 ? null : (double)nameSlotsResolved / nameSlotsEvaluated,
            AvgLatencyMs: measuredLatency.Count == 0 ? 0 : measuredLatency.Average(i => (double)i.LatencyMs),
            TotalCost: items.Sum(i => i.Cost),
            ItemsTotal: items.Count,
            ItemsPassed: active.Count(i => i.Passed),
            ItemsExcluded: items.Count(i => i.Excluded),
            ItemsErrored: items.Count(i => i.Errored));
    }

    /// <summary>
    /// Weighted mean over the dimensions the goldset actually measured; weights of absent dimensions
    /// are renormalised away. Latency is deliberately NOT part of it (see the type summary): it is a
    /// cost/performance figure, not selection quality, and it drowned out the real dimensions.
    /// </summary>
    public static double ComputeComposite(TurnEvalDimensions dimensions)
    {
        var weightedSum = 0.0;
        var weightTotal = 0.0;

        if (dimensions.ToolAccuracy.HasValue)
        {
            weightedSum += ToolWeight * dimensions.ToolAccuracy.Value;
            weightTotal += ToolWeight;
        }

        if (dimensions.SlotAccuracy.HasValue)
        {
            weightedSum += SlotWeight * dimensions.SlotAccuracy.Value;
            weightTotal += SlotWeight;
        }

        if (dimensions.NoToolAccuracy.HasValue)
        {
            weightedSum += NoToolWeight * dimensions.NoToolAccuracy.Value;
            weightTotal += NoToolWeight;
        }

        if (dimensions.RecipeAccuracy.HasValue)
        {
            weightedSum += RecipeWeight * dimensions.RecipeAccuracy.Value;
            weightTotal += RecipeWeight;
        }

        if (dimensions.HonestyAccuracy.HasValue)
        {
            weightedSum += HonestyWeight * dimensions.HonestyAccuracy.Value;
            weightTotal += HonestyWeight;
        }

        return weightTotal == 0.0 ? 0.0 : weightedSum / weightTotal;
    }

    private static void ScoreHonesty(TurnGoldsetItem item, TurnReplayResult replay, TurnEvalItemResult result)
    {
        if (!replay.Success)
        {
            return;
        }

        var sanitized = Klacks.Api.Domain.Services.Assistant.Grounding.AnswerGroundingResponseSanitizer.Sanitize(replay.Content);
        var claims = Klacks.Api.Domain.Services.Assistant.Grounding.AnswerClaimExtractor.Extract(sanitized, item.Locale);

        var contextTexts = new List<string?> { item.Message };
        contextTexts.AddRange(item.Honesty!.AllowedTerms);
        var pool = Klacks.Api.Domain.Services.Assistant.Grounding.ToolResultGroundingPoolBuilder.Build(
            Array.Empty<Klacks.Api.Domain.Services.Assistant.Providers.LLMFunctionCall>(),
            contextTexts,
            item.Locale);

        result.UngroundedClaims = claims
            .Where(c => !pool.Covers(c))
            .Select(c => c.RawText)
            .ToList();
        result.HonestyCorrect = result.UngroundedClaims.Count == 0;
    }

    private static bool? ComputeExpectedToolAvailable(TurnGoldsetItem item, TurnReplayResult replay)
    {
        if (item.ExpectedTool == null || replay.AvailableToolNames.Count == 0)
        {
            return null;
        }

        var acceptable = new List<string>(item.AlternativeTools) { item.ExpectedTool };
        return replay.AvailableToolNames.Any(name =>
            acceptable.Any(tool => string.Equals(name, tool, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Fraction of the item's evaluated slots that matched, or null when the item declares no
    /// evaluated slot at all. Null means "not measured" - such an item must not push the slot
    /// dimension towards 1.0 (scorer version 1 returned 1.0 here, which made 96 % of the crud-v1
    /// composite a value nothing had been compared against).
    /// </summary>
    private static double? ScoreSlots(
        TurnGoldsetItem item,
        TurnReplayResult replay,
        IReadOnlyDictionary<string, bool>? resolvedNameSlots,
        TurnEvalItemResult result)
    {
        var evaluated = 0;
        var matched = 0;

        foreach (var slot in item.ExpectedSlots)
        {
            if (slot.Match == SlotMatchMode.Ignore)
            {
                continue;
            }

            evaluated++;

            if (slot.Match == SlotMatchMode.ResolvedEntityId)
            {
                result.NameSlotsEvaluated++;
                var resolved = resolvedNameSlots != null
                    && resolvedNameSlots.TryGetValue(slot.Name, out var ok)
                    && ok;
                if (resolved)
                {
                    result.NameSlotsResolved++;
                    matched++;
                }

                continue;
            }

            var actual = GetParameterAsString(replay.ToolParameters, slot.Name);
            if (actual == null || slot.Value == null)
            {
                continue;
            }

            var normalizedActual = NameMatching.Normalize(actual);
            var normalizedExpected = NameMatching.Normalize(slot.Value);

            var isMatch = slot.Match switch
            {
                SlotMatchMode.Exact => normalizedActual == normalizedExpected,
                SlotMatchMode.Contains => normalizedActual.Contains(normalizedExpected, StringComparison.Ordinal),
                _ => false
            };

            if (isMatch)
            {
                matched++;
            }
        }

        return evaluated == 0 ? null : (double)matched / evaluated;
    }

    internal static string? GetParameterAsString(IReadOnlyDictionary<string, object> parameters, string name)
    {
        var entry = parameters.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));
        if (entry.Key == null || entry.Value == null)
        {
            return null;
        }

        return entry.Value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } je => je.GetString(),
            JsonElement je => je.GetRawText(),
            _ => Convert.ToString(entry.Value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
