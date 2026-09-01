// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure scoring logic for turn-selection evals: compares the replayed tool choice and
/// arguments against the goldset expectation, aggregates per-item results into
/// dimensions and computes the weighted composite. Entity resolution for
/// resolved-entity-id slots happens outside (async) and is passed in as precomputed
/// verdicts keyed by slot name.
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.Skills;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnEvalScorer
{
    private const double ToolWeight = 0.45;
    private const double SlotWeight = 0.20;
    private const double NoToolWeight = 0.10;
    private const double LatencyWeight = 0.10;
    private const double HonestyWeight = 0.15;
    private const double LatencyNormalizerMs = 8000.0;

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

            if (item.Honesty != null)
            {
                ScoreHonesty(item, replay, result);
                result.Passed = !result.Excluded && result.NoToolCorrect == true && result.HonestyCorrect == true;
                return result;
            }

            result.Passed = !result.Excluded && result.NoToolCorrect == true;
            return result;
        }

        // W4 multi-step: a check-then-act sequence (list_* first, then create_*/update_*/delete_*)
        // counts as a hit on the action call. Legacy single-shot replays have no ToolCalls and fall
        // back to ChosenTool/ToolParameters.
        var matchedCall = replay.ToolCalls.FirstOrDefault(c => MatchesExpectedTool(c.Name, item));
        var toolHit = replay.Success && matchedCall != null;
        if (!toolHit && replay.ToolCalls.Count == 0 && replay.ChosenTool != null && MatchesExpectedTool(replay.ChosenTool, item))
        {
            toolHit = true;
            matchedCall = new TurnReplayToolCall { Name = replay.ChosenTool, Parameters = replay.ToolParameters };
        }

        result.ToolHit = toolHit;

        if (toolHit)
        {
            result.SlotScore = ScoreSlots(item, matchedCall!.Parameters, resolvedNameSlots, result);
        }

        result.Passed = !result.Excluded && toolHit && (result.SlotScore ?? 1.0) >= 1.0;
        return result;
    }

    private static bool MatchesExpectedTool(string? chosenTool, TurnGoldsetItem item)
    {
        return chosenTool != null
            && (string.Equals(chosenTool, item.ExpectedTool, StringComparison.OrdinalIgnoreCase)
                || item.AlternativeTools.Any(t => string.Equals(chosenTool, t, StringComparison.OrdinalIgnoreCase)));
    }

    public static TurnEvalDimensions Aggregate(IReadOnlyList<TurnEvalItemResult> items)
    {
        var active = items.Where(i => !i.Excluded).ToList();
        var toolItems = active.Where(i => i.ExpectedTool != null).ToList();
        var noToolItems = active.Where(i => i.ExpectedTool == null && i.ExpectedRecipe == null).ToList();
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
            NameResolutionAccuracy: nameSlotsEvaluated == 0 ? null : (double)nameSlotsResolved / nameSlotsEvaluated,
            AvgLatencyMs: measuredLatency.Count == 0 ? 0 : measuredLatency.Average(i => (double)i.LatencyMs),
            TotalCost: items.Sum(i => i.Cost),
            ItemsTotal: items.Count,
            ItemsPassed: active.Count(i => i.Passed),
            ItemsExcluded: items.Count(i => i.Excluded),
            ItemsErrored: items.Count(i => i.Errored));
    }

    public static double ComputeComposite(TurnEvalDimensions dimensions)
    {
        var latencyScore = 1.0 - Math.Clamp(dimensions.AvgLatencyMs / LatencyNormalizerMs, 0.0, 1.0);

        var weightedSum = LatencyWeight * latencyScore;
        var weightTotal = LatencyWeight;

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

        if (dimensions.HonestyAccuracy.HasValue)
        {
            weightedSum += HonestyWeight * dimensions.HonestyAccuracy.Value;
            weightTotal += HonestyWeight;
        }

        return weightedSum / weightTotal;
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

    private static double ScoreSlots(
        TurnGoldsetItem item,
        IReadOnlyDictionary<string, object> toolParameters,
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

            var actual = GetParameterAsString(toolParameters, slot.Name);
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

        return evaluated == 0 ? 1.0 : (double)matched / evaluated;
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
