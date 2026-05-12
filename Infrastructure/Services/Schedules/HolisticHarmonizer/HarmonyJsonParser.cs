// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Static helpers for parsing LLM JSON responses into MutationBatch collections.
 * Tolerates string-encoded integers and extracts balanced JSON objects from prose-wrapped responses.
 */

using System.Text.Json;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

internal static class HarmonyJsonParser
{
    private const int MaxBatchesPerResponse = 3;

    internal static List<MutationBatch> TryParseBatches(string raw, int maxStepsPerBatch, int iterationIndex, ILogger logger, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Model returned empty content. Reasoning models often consume the entire token budget on internal chain-of-thought; try a non-reasoning model (e.g. deepseek-v4-flash, llama-3-3-70b-versatile) or raise max_tokens.";
            return [];
        }

        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            error = "No JSON object found in LLM response.";
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("batches", out var batchesEl) || batchesEl.ValueKind != JsonValueKind.Array)
            {
                error = "JSON missing 'batches' array.";
                return [];
            }

            var result = new List<MutationBatch>();
            foreach (var batchEl in batchesEl.EnumerateArray())
            {
                if (result.Count >= MaxBatchesPerResponse)
                {
                    break;
                }
                if (!TryReadBatch(batchEl, maxStepsPerBatch, iterationIndex, out var batch))
                {
                    continue;
                }
                result.Add(batch);
            }
            return result;
        }
        catch (JsonException ex)
        {
            error = $"JSON parse failed: {ex.Message}";
            logger.LogWarning(ex, "Holistic Harmonizer LLM JSON parse failed; raw response length {Length}", raw.Length);
            return [];
        }
    }

    private static bool TryReadBatch(JsonElement el, int maxStepsPerBatch, int iterationIndex, out MutationBatch batch)
    {
        batch = default!;
        if (el.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var intent = el.TryGetProperty("intent", out var intentEl) && intentEl.ValueKind == JsonValueKind.String
            ? intentEl.GetString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(intent))
        {
            return false;
        }

        if (!el.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var steps = new List<PlanCellSwap>();
        foreach (var stepEl in stepsEl.EnumerateArray())
        {
            if (steps.Count >= maxStepsPerBatch)
            {
                break;
            }
            if (TryReadSwap(stepEl, out var swap))
            {
                steps.Add(swap);
            }
        }

        if (steps.Count == 0)
        {
            return false;
        }

        batch = new MutationBatch(Guid.NewGuid(), intent, iterationIndex, steps);
        return true;
    }

    private static bool TryReadSwap(JsonElement el, out PlanCellSwap swap)
    {
        swap = default!;
        if (el.ValueKind != JsonValueKind.Object)
        {
            return false;
        }
        if (!TryReadIntField(el, "rowA", out var rowA)) return false;
        if (!TryReadIntField(el, "dayA", out var dayA)) return false;
        if (!TryReadIntField(el, "rowB", out var rowB)) return false;
        if (!TryReadIntField(el, "dayB", out var dayB)) return false;
        var reason = el.TryGetProperty("reason", out var reasonEl) && reasonEl.ValueKind == JsonValueKind.String
            ? reasonEl.GetString() ?? string.Empty
            : string.Empty;
        swap = new PlanCellSwap(rowA, dayA, rowB, dayB, reason);
        return true;
    }

    /// <summary>
    /// Reads an integer property tolerantly: accepts JSON numbers natively, falls back to
    /// parsing string values like <c>"3"</c>. Some open-source LLMs (Apertus, smaller llama
    /// variants) emit coordinates as strings even when the schema asks for ints; rejecting
    /// them would waste an iteration. Returns false when the property is missing or the
    /// value cannot be coerced to a non-negative int.
    /// </summary>
    private static bool TryReadIntField(JsonElement parent, string name, out int value)
    {
        value = 0;
        if (!parent.TryGetProperty(name, out var el))
        {
            return false;
        }
        if (el.ValueKind == JsonValueKind.Number)
        {
            return el.TryGetInt32(out value);
        }
        if (el.ValueKind == JsonValueKind.String)
        {
            return int.TryParse(el.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value);
        }
        return false;
    }

    /// <summary>
    /// Returns the first balanced JSON object found in <paramref name="raw"/>. Walks the
    /// content character by character tracking brace depth so trailing prose, markdown code
    /// fences, or explanatory backticks after the closing brace are dropped instead of being
    /// captured by a greedy regex. Strings (including escaped quotes) are skipped to keep
    /// braces inside them from skewing the depth counter.
    /// </summary>
    internal static string? ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var start = raw.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        for (var i = start; i < raw.Length; i++)
        {
            var c = raw[i];
            if (inString)
            {
                if (c == '\\' && i + 1 < raw.Length)
                {
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inString = false;
                }
                continue;
            }
            if (c == '"')
            {
                inString = true;
                continue;
            }
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return raw.Substring(start, i - start + 1);
                }
            }
        }
        return null;
    }

    internal static int CountSteps(IReadOnlyList<MutationBatch> batches)
    {
        var total = 0;
        for (var i = 0; i < batches.Count; i++)
        {
            total += batches[i].Steps.Count;
        }
        return total;
    }
}
