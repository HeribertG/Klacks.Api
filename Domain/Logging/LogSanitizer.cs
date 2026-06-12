// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Logging;

/// <summary>
/// Neutralizes user-controlled values before they are written to logs by stripping carriage
/// return and line feed characters, preventing log forging / log injection (CWE-117).
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Returns the value with CR and LF characters removed so it cannot inject forged log lines.
    /// </summary>
    /// <param name="value">The untrusted value to neutralize for safe logging.</param>
    public static string ForLog(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a partially redacted form of an email address for logging: the local part is
    /// reduced to its first character so the personally identifiable identity is not written to
    /// logs, while the domain is kept for operational debugging. CR/LF are stripped as well.
    /// </summary>
    /// <param name="value">The untrusted email address to mask before logging.</param>
    public static string MaskEmail(this string? value)
    {
        var sanitized = value.ForLog();
        if (string.IsNullOrEmpty(sanitized))
        {
            return string.Empty;
        }

        var atIndex = sanitized.IndexOf('@');
        if (atIndex <= 0)
        {
            return $"{sanitized[0]}***";
        }

        var domain = sanitized[(atIndex + 1)..];
        return $"{sanitized[0]}***@{domain}";
    }
}
