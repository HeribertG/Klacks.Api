// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the correction turn carries into the chat loop: the volatile note (always) and a ready-to-send
/// clarification question with its two candidates (only when the re-routing had no clear winner). The
/// undo invocation is added by the task that introduces the inverse-skill resolver.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GracefulCorrectionOutcome(
    string ContextNote,
    string? ClarificationReply,
    IReadOnlyList<string> ClarificationSkillNames);
