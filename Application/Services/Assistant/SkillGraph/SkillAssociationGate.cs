// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a user message looks like a request with several parts, the only kind the skill-graph association
/// may act on. The signal is language-independent: a clause or sentence mark (Latin, CJK or Arabic) followed by more
/// text. A single short question keeps exactly its retrieved skills, because on a full tool budget every associated
/// skill displaces a retrieved one, and on single questions that cost the holdout set two hits (IT 2026-11-14).
/// Simulated on all goldsets the same day: multi-tool completeness 54 -> 60 of 100, single-target sets 1059 -> 1061,
/// no set lower.
/// </summary>

using System.Text.RegularExpressions;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public static partial class SkillAssociationGate
{
    private const int MaxInspectedLength = 2000;

    [GeneratedRegex(@"[.?!;,。？！，、؟،]\s*\S", RegexOptions.NonBacktracking)]
    private static partial Regex PartSeparator();

    /// <summary>
    /// True when the message, cut to its first 2000 characters, holds a clause or sentence mark followed by more text; a blank message is false.
    /// </summary>
    /// <param name="userMessage">The user's message of this turn</param>
    public static bool LooksMultiPart(string? userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return false;
        }

        var inspected = userMessage.Length > MaxInspectedLength
            ? userMessage[..MaxInspectedLength]
            : userMessage;
        return PartSeparator().IsMatch(inspected.Trim());
    }
}
