// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingConfirmationStore
{
    string Create(Guid userId, string skillName, IReadOnlyDictionary<string, object> parameters);

    PendingConfirmation? Consume(string token, Guid userId, string? expectedSkillName = null);

    PendingConfirmationHandle? PeekLatestForUser(
        Guid userId,
        TimeSpan maxAge,
        string purpose = PendingConfirmationPurposes.GateReplay);

    void CreateProposalHint(Guid userId, string applySkillName);

    void DiscardProposalHints(Guid userId, string? applySkillName = null);
}
