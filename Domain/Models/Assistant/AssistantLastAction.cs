// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The assistant's immediately preceding turn in one conversation: the user message that produced it,
/// every tool call it made, and a short excerpt of the answer. This is the anchor of the graceful
/// correction path - written synchronously at the end of the turn, never by the fire-and-forget
/// trajectory capture, which may land after the next turn has already started.
/// A turn without a tool call does not replace the record; it marks it superseded, so a correction can
/// only ever refer to the turn that literally preceded it.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class AssistantLastAction
{
    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string UserMessage { get; set; } = string.Empty;

    public IReadOnlyList<AssistantLastActionCall> Calls { get; set; } = [];

    public string AssistantAnswerExcerpt { get; set; } = string.Empty;

    public DateTime CreateTimeUtc { get; set; }

    /// <summary>
    /// Set when a later turn made no tool call, or when this record only carries clarification pins.
    /// A superseded record can never anchor a correction (gate G0) but its pins are still read.
    /// </summary>
    public DateTime? SupersededAtUtc { get; set; }

    /// <summary>
    /// The two candidate skills a clarification question offered, so the next turn's toolset can pin
    /// them whichever of the two the user names. Empty on an ordinary record.
    /// </summary>
    public IReadOnlyList<string> ClarificationSkillNames { get; set; } = [];

    /// <summary>
    /// True while this record may anchor a correction: it made at least one tool call, was not
    /// superseded, and is younger than the correction window.
    /// </summary>
    public bool CanAnchorCorrection(DateTime nowUtc) =>
        Calls.Count > 0
        && SupersededAtUtc == null
        && nowUtc - CreateTimeUtc <= TimeSpan.FromMinutes(Constants.GracefulCorrectionDefaults.CorrectionWindowMinutes);

    /// <summary>
    /// The skills the successful calls of this record ran, in call order with every name at its LAST occurrence,
    /// so the final entry is the most recent skill. Failed calls never count. A superseded record still answers,
    /// which bounds the advisory follow-through by the record's TTL rather than by the immediately preceding turn
    /// (an interview often needs several tool-free turns). With resolveReplayedSkills a
    /// wrapper call (confirm_pending_action) contributes the skill it replayed instead of its own name; the
    /// live chat and the headless replay both read the previous turn through this one method, so they cannot
    /// diverge.
    /// </summary>
    /// <param name="resolveReplayedSkills">Whether a wrapper call is named by the skill it replayed</param>
    public IReadOnlyList<string> SuccessfulSkillNames(bool resolveReplayedSkills)
    {
        var names = Calls
            .Where(call => call.Success)
            .Select(call => resolveReplayedSkills && !string.IsNullOrWhiteSpace(call.ReplayedSkillName)
                ? call.ReplayedSkillName!
                : call.SkillName)
            .ToList();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastOccurrences = new List<string>(names.Count);
        for (var index = names.Count - 1; index >= 0; index--)
        {
            if (seen.Add(names[index]))
            {
                lastOccurrences.Add(names[index]);
            }
        }

        lastOccurrences.Reverse();
        return lastOccurrences;
    }

    /// <summary>
    /// The skills the same-skill continuation may keep: the successful calls of this record with a wrapper call named
    /// by the skill it replayed, most recent last - but only while the record still describes the IMMEDIATELY
    /// preceding turn. Once a tool-free turn or a clarification superseded it, the user has moved on and nothing is
    /// continued. Callers pass null instead on a correction turn, which must not get the corrected skill back.
    /// </summary>
    public IReadOnlyList<string>? ContinuationSkillNames() =>
        SupersededAtUtc == null ? SuccessfulSkillNames(resolveReplayedSkills: true) : null;
}
