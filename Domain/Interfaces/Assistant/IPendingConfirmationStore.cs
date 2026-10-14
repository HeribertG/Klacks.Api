// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Short-lived store for pending skill confirmations: Create issues a one-time token bound
/// to user, skill and the full invocation parameters; Consume validates and invalidates it
/// in a single step and returns the stored invocation so it can be replayed exactly;
/// PeekLatestForUser inspects (without consuming) whether the user still has an outstanding
/// confirmation, so the orchestrator can resurface its token on the confirmation turn.
/// The same store also holds proposal hints (CreateProposalHint / DiscardProposalHints): a
/// successful read-only propose_* skill records only WHICH apply_* skill the user may confirm
/// next, with no invocation parameters. Both kinds live in one table, told apart by their
/// purpose (PendingConfirmationPurposes), and are read by two independent callers — the
/// gate-replay path never sees a proposal hint and vice versa.
/// A correction undo is written through the same Create with its own purpose and dropped again by
/// DiscardCorrectionUndo, because it is the one token whose validity ends with the very next user turn.
/// Every token carries the channel that issued it (PendingConfirmationChannels). Consume refuses a token of another
/// channel exactly like one of another user - the row is deleted first and nothing is returned - and PeekLatestForUser
/// only sees rows of the asked channel, so the chat never resurfaces or redeems a token an external agent caused.
/// Every issuer that has a SkillExecutionContext passes its ConfirmationChannel; the default is only for the chat.
/// The gate also stores the ConfirmationBinding of the preview the user is asked about; Consume returns it with the
/// invocation so the replay can refuse when what it would do differs from what was shown. A stored binding that cannot
/// be read makes Consume return null (the token is void) instead of silently reading as "nothing bound".
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingConfirmationStore
{
    string Create(
        Guid userId,
        string skillName,
        IReadOnlyDictionary<string, object> parameters,
        string purpose = PendingConfirmationPurposes.GateReplay,
        string channel = PendingConfirmationChannels.Chat,
        ConfirmationBinding? binding = null);

    PendingConfirmation? Consume(
        string token,
        Guid userId,
        string? expectedSkillName = null,
        string channel = PendingConfirmationChannels.Chat);

    PendingConfirmationHandle? PeekLatestForUser(
        Guid userId,
        TimeSpan maxAge,
        string purpose = PendingConfirmationPurposes.GateReplay,
        string channel = PendingConfirmationChannels.Chat);

    void CreateProposalHint(Guid userId, string applySkillName);

    void DiscardProposalHints(Guid userId, string? applySkillName = null);

    void DiscardCorrectionUndo(Guid userId);

    /// <summary>
    /// Drops the outstanding confirmations of the user that carry one of the given tokens, whatever their
    /// purpose. Tokens that are unknown, already redeemed or belong to somebody else are left alone.
    /// </summary>
    /// <param name="userId">The user the tokens were issued to</param>
    /// <param name="tokens">The tokens to drop</param>
    void DiscardByTokens(Guid userId, IReadOnlyCollection<string> tokens);
}
