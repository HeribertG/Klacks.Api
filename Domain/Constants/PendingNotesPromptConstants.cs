// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The per-turn hint that tells the model the current user has undelivered pending notes and which skill
/// reads them. It is an instruction to CALL a tool, so ToolDirectedPromptFilter removes it from every
/// tool-less request: a model that is told to call a tool it does not have deliberates about it and
/// writes no answer (live 2026-09-24, recipe confirmation step).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PendingNotesPromptConstants
{
    public const string Marker = "[PENDING_NOTES:";

    public const string HintTemplate =
        Marker + " {0}] You have {0} undelivered note(s) stashed for this user. Call " + SkillNames.ManagePendingNotes
        + " with action 'read' to read them, relay them to the user naturally, then call " + SkillNames.ManagePendingNotes
        + " with action 'mark_delivered' and their ids so they are not delivered again.";
}
