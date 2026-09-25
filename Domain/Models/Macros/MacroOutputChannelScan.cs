// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of scanning a macro script for its OUTPUT statements without executing it.
/// </summary>
/// <param name="LiteralChannels">Channel numbers of every OUTPUT whose channel is a plain number literal, in source order</param>
/// <param name="NonLiteralChannelCount">Number of OUTPUT statements whose channel is computed and therefore cannot be checked</param>
/// <param name="FailureMessage">Why the scan could not complete (tokenizer error or timeout); null when it succeeded</param>

namespace Klacks.Api.Domain.Models.Macros;

public record MacroOutputChannelScan(
    IReadOnlyList<int> LiteralChannels,
    int NonLiteralChannelCount,
    string? FailureMessage)
{
    public bool Succeeded => FailureMessage == null;

    public static MacroOutputChannelScan Failure(string message) => new(Array.Empty<int>(), 0, message);
}
