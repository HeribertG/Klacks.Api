// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one check for a language pack code or locale tag that becomes a directory name below the language pack
/// directory: letters, digits, '_' and '-' only, so a value can never carry a path separator, "..", a drive
/// or a line break. Every reader or writer that builds a path from a pack code taken from a request, a route,
/// a store bundle or a manifest asks here first.
/// </summary>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Common;

public static partial class LanguagePluginCode
{
    /// <summary>
    /// Longest accepted code, the practical BCP-47 tag length of RFC 5646.
    /// </summary>
    public const int MaxLength = 35;

    private const string CodePattern = @"^[A-Za-z0-9_-]{1,35}\z";

    /// <summary>
    /// True when the code is a non-empty run of letters, digits, '_' and '-' of at most <see cref="MaxLength"/> characters.
    /// </summary>
    /// <param name="code">Pack code or locale tag such as "xx", "zh-CN" or "sr-Latn"; null or blank is invalid</param>
    public static bool IsValid(string? code)
    {
        return !string.IsNullOrEmpty(code) && CodeRegex().IsMatch(code);
    }

    [GeneratedRegex(CodePattern, RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();
}
