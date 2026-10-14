// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Plain-text form of an e-mail body for the e-mail skills: the text part when there is one, otherwise the HTML part with
/// scripts, styles and tags removed and entities decoded. Shared by read_email and translate_email, whose translated mail
/// carries only the HTML part when the original was HTML.
/// </summary>
/// <param name="text">The plain-text part of the body, if any</param>
/// <param name="html">The HTML part of the body, if any</param>

using System.Net;
using System.Text.RegularExpressions;

namespace Klacks.Api.Application.Skills;

public static class EmailBodyPlainText
{
    private const string ScriptStyleBlockPattern = @"<(script|style)\b[^>]*>.*?</\1\s*>";
    private const string LineBreakTagPattern = @"<\s*(br\s*/?|/p|/div|/li|/tr|/h[1-6])\s*>";
    private const string AnyTagPattern = @"<[^>]+>";
    private const string ExcessBlankLinesPattern = @"(\s*\n){3,}";
    private const string ParagraphSeparator = "\n\n";
    private const string LineBreak = "\n";
    private static readonly TimeSpan StripTimeout = TimeSpan.FromSeconds(2);

    public static string From(string? text, string? html) =>
        !string.IsNullOrWhiteSpace(text) ? text.Trim() : StripHtml(html);

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var stripped = Regex.Replace(html, ScriptStyleBlockPattern, string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline, StripTimeout);
        stripped = Regex.Replace(stripped, LineBreakTagPattern, LineBreak, RegexOptions.IgnoreCase, StripTimeout);
        stripped = Regex.Replace(stripped, AnyTagPattern, string.Empty, RegexOptions.Singleline, StripTimeout);
        stripped = WebUtility.HtmlDecode(stripped);
        stripped = Regex.Replace(stripped, ExcessBlankLinesPattern, ParagraphSeparator, RegexOptions.None, StripTimeout);
        return stripped.Trim();
    }
}
