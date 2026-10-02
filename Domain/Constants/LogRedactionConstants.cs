// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Placeholders and markers used when a request URI is written to a log. The query placeholder
/// matches the form Microsoft.Extensions.Http uses for its own redacted request lines.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class LogRedactionConstants
{
    public const char QueryStart = '?';
    public const string RedactedQuery = "?*";
    public const string RedactedBotTokenSegment = "/bot***";
    public const string RedactedUserInfo = "://***@";
}
