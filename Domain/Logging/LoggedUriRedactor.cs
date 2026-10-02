// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a request URI into the form that may be written to a log: the query string is replaced by
/// "?*" (API keys such as Google's "key=" or WeChat's "access_token=" live there) and a Telegram style
/// "/bot&lt;id&gt;:&lt;token&gt;" path segment is masked, because the bot token is part of the path.
/// Credentials in the authority ("user:password@") are masked too.
/// CR/LF are stripped as well, so the result is also safe against log forging.
/// </summary>
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Logging;

public static partial class LoggedUriRedactor
{
    /// <summary>
    /// Returns the loggable form of an absolute or relative request URI.
    /// </summary>
    /// <param name="uri">Request URI as a string, possibly carrying credentials in query or path</param>
    public static string RedactUriForLog(this string? uri)
    {
        var sanitized = uri.ForLog();
        if (sanitized.Length == 0)
        {
            return sanitized;
        }

        var queryIndex = sanitized.IndexOf(LogRedactionConstants.QueryStart);
        var withoutQuery = queryIndex < 0
            ? sanitized
            : sanitized[..queryIndex] + LogRedactionConstants.RedactedQuery;

        var withoutUserInfo = UserInfo().Replace(withoutQuery, LogRedactionConstants.RedactedUserInfo);
        return BotTokenSegment().Replace(withoutUserInfo, LogRedactionConstants.RedactedBotTokenSegment);
    }

    /// <summary>
    /// Returns the loggable form of a request URI object.
    /// </summary>
    /// <param name="uri">Request URI, possibly carrying credentials in query or path</param>
    public static string RedactUriForLog(this Uri? uri) =>
        (uri is null ? null : uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.OriginalString).RedactUriForLog();

    [GeneratedRegex(@"://[^/?#@]+@", RegexOptions.CultureInvariant)]
    private static partial Regex UserInfo();

    [GeneratedRegex(@"/bot\d+:[^/?#]+", RegexOptions.CultureInvariant)]
    private static partial Regex BotTokenSegment();
}
