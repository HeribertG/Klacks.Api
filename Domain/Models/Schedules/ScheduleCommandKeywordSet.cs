// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The eight planning-command tokens currently in effect (admin-configurable via Settings,
/// falling back to the English defaults FREE/-FREE/EARLY/-EARLY/LATE/-LATE/NIGHT/-NIGHT).
/// </summary>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record ScheduleCommandKeywordSet
{
    public required string FreeToken { get; init; }

    public required string NegFreeToken { get; init; }

    public required string EarlyToken { get; init; }

    public required string NegEarlyToken { get; init; }

    public required string LateToken { get; init; }

    public required string NegLateToken { get; init; }

    public required string NightToken { get; init; }

    public required string NegNightToken { get; init; }

    public IReadOnlySet<string> ValidTokens => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        FreeToken, NegFreeToken, EarlyToken, NegEarlyToken, LateToken, NegLateToken, NightToken, NegNightToken
    };

    public bool TryResolveToken(string raw, out string resolved)
    {
        var match = ValidTokens.FirstOrDefault(t => string.Equals(t, raw, StringComparison.OrdinalIgnoreCase));
        resolved = match ?? string.Empty;
        return match != null;
    }
}
