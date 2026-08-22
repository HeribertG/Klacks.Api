// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Neutralizes the tool-result delimiters inside content that is about to be placed BETWEEN those
/// delimiters. Without this, a web-search snippet, an e-mail body or an ERP-imported field could
/// simply contain the closing delimiter and continue with forged results or forged instructions that
/// the model would read as coming from this system. Applied to every result, not only the untrusted
/// ones, because externally sourced strings (imported names, references, free-text notes) also reach
/// the model through ordinary read skills.
/// </summary>
/// <param name="text">Raw tool-result or function-name text that is about to be embedded in a block</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ToolResultSanitizer
{
    private static readonly string[] Delimiters =
    [
        ToolResultMarkers.BlockFooter,
        ToolResultMarkers.BlockHeader,
        ToolResultMarkers.ResultClose,
        ToolResultMarkers.ResultOpenPrefix
    ];

    public static string EscapeDelimiters(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var result = text;
        foreach (var delimiter in Delimiters)
        {
            result = result.Replace(
                delimiter, ToolResultMarkers.EscapedMarkerReplacement, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
