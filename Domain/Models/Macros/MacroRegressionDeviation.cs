// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One place where an extended macro copy no longer produces what the original produces.
/// </summary>
/// <param name="SampleDescription">Human-readable description of the test input (weekday, times, flags, rates)</param>
/// <param name="Channel">The OUTPUT channel whose value differs</param>
/// <param name="OriginalValue">The value the original macro produced on this channel (non-zero for a surcharge channel, any value including 0 for the result channel 1), or null when the original produced none</param>
/// <param name="CopyValue">The value the copy produced on this channel, or null when the copy produced none</param>
/// <param name="AcceptedTotal">Result channel only: the second value the copy could have produced, the original result plus the surcharges the copy adds on channels the original leaves at zero; null when the copy adds no surcharge</param>

namespace Klacks.Api.Domain.Models.Macros;

public record MacroRegressionDeviation(
    string SampleDescription,
    int Channel,
    decimal? OriginalValue,
    decimal? CopyValue,
    decimal? AcceptedTotal = null);
