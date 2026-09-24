// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Joins the two optional clarification context blocks of the regular analysis notification (the answer
/// context of a resolved question and the note of the notifier) with a blank line. Language-neutral: the
/// blocks themselves are already localized by ClarificationTextService.
/// </summary>
/// <param name="first">First context block, possibly null or blank</param>
/// <param name="second">Second context block, possibly null or blank</param>

namespace Klacks.Api.Infrastructure.Inbound;

internal static class ClarificationContextBlocks
{
    private const string BlockSeparator = "\n\n";

    internal static string? Join(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.IsNullOrWhiteSpace(second) ? null : second;
        }

        return string.IsNullOrWhiteSpace(second) ? first : first + BlockSeparator + second;
    }
}
