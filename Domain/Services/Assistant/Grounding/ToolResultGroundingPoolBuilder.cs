// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the grounding pool of a turn from the structured DataJson fragments of successful
/// data-bearing function calls plus the legitimate conversational context texts (user message,
/// entity grounding block, injected memories, page context, sent history). Collects normalized
/// numbers/dates/UUIDs, array counts and per-column sums; flags EmptyDataDespiteSuccess when a
/// successful data call carries no meaningful values (the visibility trap: empty results must
/// never count as coverage).
/// </summary>
/// <param name="calls">All function calls of the turn; only successful Data calls contribute.</param>
/// <param name="contextTexts">Texts the model legitimately saw (message, grounding block, memories, page context, history).</param>
/// <param name="language">ISO language of the turn, used for date parsing in context texts.</param>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.Grounding;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.Grounding;

public static class ToolResultGroundingPoolBuilder
{
    private static readonly string[] CountLikePropertySuffixes = ["count", "total", "totalcount", "page", "pagesize", "pagenumber"];

    public static GroundingPool Build(
        IReadOnlyList<LLMFunctionCall> calls,
        IReadOnlyList<string?> contextTexts,
        string? language)
    {
        var pool = new GroundingPool();
        var corpus = new StringBuilder();

        foreach (var call in calls)
        {
            if (!call.Success || call.ResultKind != LLMFunctionResultKind.Data)
            {
                continue;
            }

            var meaningful = false;
            foreach (var fragment in call.DataJson)
            {
                meaningful |= AddJsonFragment(pool, corpus, fragment);
            }

            if (!meaningful)
            {
                pool.EmptyDataDespiteSuccess = true;
            }
        }

        foreach (var text in contextTexts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            corpus.Append(text.ToLowerInvariant()).Append('\n');
            foreach (var claim in AnswerClaimExtractor.Extract(text, language))
            {
                AddClaimReadings(pool, claim);
            }
        }

        pool.TextCorpus = corpus.ToString();
        if (pool.Numbers.Count > GroundingPool.PairArithmeticNumberCap)
        {
            pool.DerivationsDisabled = true;
        }

        return pool;
    }

    private static void AddClaimReadings(GroundingPool pool, AnswerClaim claim)
    {
        foreach (var reading in claim.Readings)
        {
            switch (claim.Kind)
            {
                case AnswerClaimKind.Uuid:
                    pool.UuidKeys.Add(reading);
                    break;
                case AnswerClaimKind.Date:
                    pool.DateKeys.Add(reading);
                    break;
                case AnswerClaimKind.Number:
                    if (decimal.TryParse(reading, System.Globalization.NumberStyles.AllowDecimalPoint,
                            System.Globalization.CultureInfo.InvariantCulture, out var value))
                    {
                        pool.AddNumber(value);
                    }

                    break;
            }
        }
    }

    private static bool AddJsonFragment(GroundingPool pool, StringBuilder corpus, string fragment)
    {
        try
        {
            using var document = JsonDocument.Parse(fragment);
            return Walk(pool, corpus, document.RootElement, propertyName: null);
        }
        catch (JsonException)
        {
            corpus.Append(fragment.ToLowerInvariant()).Append('\n');
            return false;
        }
    }

    private static bool Walk(GroundingPool pool, StringBuilder corpus, JsonElement element, string? propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var meaningfulObject = false;
                foreach (var property in element.EnumerateObject())
                {
                    meaningfulObject |= Walk(pool, corpus, property.Value, property.Name);
                }

                return meaningfulObject;

            case JsonValueKind.Array:
                return WalkArray(pool, corpus, element);

            case JsonValueKind.Number:
                var number = element.GetDecimal();
                pool.AddNumber(number);
                return number != 0 && !IsCountLike(propertyName);

            case JsonValueKind.String:
                return AddStringValue(pool, corpus, element.GetString() ?? string.Empty);

            default:
                return false;
        }
    }

    private static bool WalkArray(GroundingPool pool, StringBuilder corpus, JsonElement array)
    {
        var length = array.GetArrayLength();
        pool.AddNumber(length);

        var columnSums = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var meaningful = false;

        foreach (var item in array.EnumerateArray())
        {
            meaningful |= Walk(pool, corpus, item, propertyName: null);

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var property in item.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    columnSums[property.Name] = columnSums.GetValueOrDefault(property.Name) + property.Value.GetDecimal();
                }
            }
        }

        foreach (var sum in columnSums.Values)
        {
            pool.AddNumber(sum);
        }

        return meaningful;
    }

    private static bool AddStringValue(GroundingPool pool, StringBuilder corpus, string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (Guid.TryParse(value, out var guid))
        {
            pool.UuidKeys.Add(guid.ToString("D"));
            return true;
        }

        if (value.Length == 10
            && DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date))
        {
            pool.DateKeys.Add(date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            return true;
        }

        if (decimal.TryParse(value, System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out var numeric))
        {
            pool.AddNumber(numeric);
            return true;
        }

        corpus.Append(value.ToLowerInvariant()).Append('\n');
        return true;
    }

    private static bool IsCountLike(string? propertyName)
    {
        if (propertyName == null)
        {
            return false;
        }

        var lower = propertyName.ToLowerInvariant();
        return CountLikePropertySuffixes.Any(lower.EndsWith);
    }
}
