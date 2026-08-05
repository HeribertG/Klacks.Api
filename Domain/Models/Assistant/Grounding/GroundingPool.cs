// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The set of values an assistant answer may legitimately state in a turn: normalized numbers,
/// dates and UUIDs collected from tool result data and from the conversational context, plus a
/// lowercased text corpus for name coverage. Number coverage tolerates rounding to 0-2 decimals,
/// column sums collected by the builder and pair arithmetic (a+b / a-b) unless derivations are
/// disabled by the size cap.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Models.Assistant.Grounding;

public sealed class GroundingPool
{
    public const int PairArithmeticNumberCap = 200;

    public HashSet<string> NumberKeys { get; } = new(StringComparer.Ordinal);

    public HashSet<string> DateKeys { get; } = new(StringComparer.Ordinal);

    public HashSet<string> UuidKeys { get; } = new(StringComparer.Ordinal);

    public List<decimal> Numbers { get; } = new();

    public string TextCorpus { get; set; } = string.Empty;

    public bool EmptyDataDespiteSuccess { get; set; }

    public bool DerivationsDisabled { get; set; }

    public void AddNumber(decimal value)
    {
        var key = value.ToString("G29", CultureInfo.InvariantCulture);
        if (NumberKeys.Add(key))
        {
            Numbers.Add(value);
        }
    }

    public bool Covers(AnswerClaim claim)
    {
        return claim.Kind switch
        {
            AnswerClaimKind.Uuid => claim.Readings.Any(UuidKeys.Contains),
            AnswerClaimKind.Date => claim.Readings.Any(DateKeys.Contains),
            AnswerClaimKind.Number => claim.Readings.Any(CoversNumberReading),
            _ => false
        };
    }

    private bool CoversNumberReading(string reading)
    {
        if (NumberKeys.Contains(reading))
        {
            return true;
        }

        if (!decimal.TryParse(reading, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        var decimals = DecimalPlaces(reading);
        if (decimals <= 2 && Numbers.Any(n => SafeRound(n, decimals) == value))
        {
            return true;
        }

        if (DerivationsDisabled || Numbers.Count > PairArithmeticNumberCap)
        {
            return false;
        }

        for (var i = 0; i < Numbers.Count; i++)
        {
            for (var j = i + 1; j < Numbers.Count; j++)
            {
                var sum = Numbers[i] + Numbers[j];
                var difference = Math.Abs(Numbers[i] - Numbers[j]);
                if (sum == value || difference == value)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int DecimalPlaces(string reading)
    {
        var dot = reading.IndexOf('.');
        return dot < 0 ? 0 : reading.Length - dot - 1;
    }

    private static decimal SafeRound(decimal value, int decimals)
    {
        try
        {
            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            return value;
        }
    }
}
