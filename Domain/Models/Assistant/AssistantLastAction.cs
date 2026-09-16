// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
}
