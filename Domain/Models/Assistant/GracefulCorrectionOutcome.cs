// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the correction turn carries into the chat loop: the volatile note (always), a ready-to-send
/// clarification question with its two candidates (only when the re-routing had no clear winner) and, on
/// the non-ambiguous path only, the inverse call the note offers to run.
/// Undo and ClarificationReply are mutually exclusive by construction (CorrectionOutcomeComposer): rule 3
/// allows exactly one yes/no offer per correction, and a turn that already asks which skill was meant must
/// not ask a second question in the same answer. Undo is DATA - whether a pending confirmation is written
/// for it is decided by the caller, which is what keeps the headless replay side-effect-free.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GracefulCorrectionOutcome(
    string ContextNote,
    string? ClarificationReply,
    IReadOnlyList<string> ClarificationSkillNames,
    SkillUndoInvocation? Undo = null,
    string? UndoneSkillLabel = null);
