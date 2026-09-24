// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wraps text that originates from an employee or from an earlier model step in an untrusted-data tag
/// pair for an LLM prompt. Any occurrence of the closing tag inside the text is neutralized to a
/// bracketed form, so the text can never break out of its block or be mistaken for a system fact.
/// </summary>
/// <param name="text">The untrusted text</param>
/// <param name="openTag">Opening tag, for example "&lt;employee_message&gt;"</param>
/// <param name="closeTag">Matching closing tag, for example "&lt;/employee_message&gt;"</param>

namespace Klacks.Api.Infrastructure.Inbound;

internal static class UntrustedTextBlock
{
    private const string NeutralizedTagOpenBracket = "[";
    private const string NeutralizedTagCloseBracket = "]";

    internal static string Wrap(string text, string openTag, string closeTag) =>
        openTag + NeutralizeClosingTag(text, closeTag) + closeTag;

    internal static string NeutralizeClosingTag(string text, string closeTag) =>
        text.Replace(
            closeTag,
            NeutralizedTagOpenBracket + closeTag[1..^1] + NeutralizedTagCloseBracket,
            StringComparison.OrdinalIgnoreCase);
}
