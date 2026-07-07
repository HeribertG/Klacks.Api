// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies LLM provider error messages as transient (worth retrying) or permanent.
/// Providers surface failures as free-text messages, so detection is marker-based:
/// rate limits, overload and gateway errors recover within seconds, everything else does not.
/// </summary>
/// <param name="errorMessage">The provider error message to classify</param>
namespace Klacks.Api.Domain.Services.Assistant;

public static class TransientProviderErrorDetector
{
    private static readonly string[] TransientMarkers =
    {
        "rate limit",
        "rate_limit",
        "429",
        "too many requests",
        "overloaded",
        "502",
        "503",
        "504",
        "timeout",
        "timed out",
        "temporarily unavailable",
        "service unavailable"
    };

    public static bool IsTransient(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        return TransientMarkers.Any(marker =>
            errorMessage.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
