// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the previous tool-calling turn off the last-action record and asks MailApplyIntentResolver whether this turn
/// asks to enter what e-mails contain. TurnPreparationService decides on it whether a fresh recipe may start; the toolset
/// guarantee reads the same skill names from its request (SuccessfulSkillNames without replay resolution), so both
/// sides judge on the same inputs. A failing read of the record costs only the mail context of the previous turn.
/// </summary>
/// <param name="lastActionStore">Persistence of the previous-action record</param>
/// <param name="userGuid">The user whose previous action is read</param>
/// <param name="conversationId">The conversation whose previous action is read</param>
/// <param name="message">The current user chat message</param>
/// <param name="logger">Logger for a failed read of the record</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class MailApplyTurnProbe
{
    internal static bool IsMailApplyTurn(
        IAssistantLastActionStore lastActionStore, Guid userGuid, string conversationId, string message, ILogger logger)
    {
        IReadOnlyList<string>? previousSkillNames = null;
        try
        {
            previousSkillNames = lastActionStore.Peek(userGuid, conversationId)
                ?.SuccessfulSkillNames(resolveReplayedSkills: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not read the previous action for the mail-apply decision of user {UserId}", userGuid);
        }

        return MailApplyIntentResolver.IsMailApply(message, previousSkillNames);
    }

    /// <summary>
    /// The volatile note of a mail-driven entry turn that runs without a recipe plan: how to enter the data in the few rounds
    /// the turn has (names go straight into the write skills, every call of one skill in a single batch because
    /// RepeatedWriteCallGuard refuses the skill in a later round). Null for every other turn.
    /// </summary>
    /// <param name="lastActionStore">Persistence of the previous-action record</param>
    /// <param name="context">The turn context with the user and the message</param>
    /// <param name="conversationId">The conversation whose previous action is read</param>
    /// <param name="plan">The recipe plan of this turn; a turn with a plan gets no note</param>
    /// <param name="logger">Logger for a failed read of the record</param>
    internal static string? NoteFor(
        IAssistantLastActionStore lastActionStore, LLMContext context, string conversationId,
        RecipeExecutionPlan? plan, ILogger logger) =>
        plan == null
        && Guid.TryParse(context.UserId, out var userGuid)
        && IsMailApplyTurn(lastActionStore, userGuid, conversationId, context.Message, logger)
            ? MailApplyDefaults.EntryTurnNote
            : null;
}
