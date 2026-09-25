// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The labels of the system-built fact lines in the inbound prompts (the affected shift, today's company
/// date and the analysed period of the clarification question prompt), written once for the composer
/// that builds the lines and for the prompt instructions that name them as facts. They occur in Klacks'
/// own prompts and are not expected in legitimate messages, so a label inside sender-written text (sender,
/// subject, body) is treated as a likely forgery; ContainsAny is the plain, case-insensitive check for
/// that, a verbatim-copy backstop and not a boundary. Labels that legitimate text contains as well (From:,
/// Date:, Subject:, e.g. in quoted reply headers) are deliberately not listed.
/// </summary>

namespace Klacks.Api.Infrastructure.Inbound;

internal static class InboundPromptLabels
{
    internal const string AffectedShift = "Affected shift:";
    internal const string Today = "Today (company local date):";
    internal const string AnalysedPeriod = "Analysed period:";
    internal const string ValueSeparator = " ";

    private static readonly string[] All = [AffectedShift, Today, AnalysedPeriod];

    internal static bool ContainsAny(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var label in All)
        {
            if (text.Contains(label, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
