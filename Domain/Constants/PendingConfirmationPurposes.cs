// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Discriminator values for a row in the single pending-confirmation store. GateReplay is the
/// original purpose: the autonomy gate intercepted a real skill invocation, stored its parameters
/// and expects confirm_pending_action to replay them. ProposalHint is the pre-attempt case: a
/// read-only propose_* skill succeeded and merely records which apply_* skill the user may confirm
/// next, so the toolset assembler can guarantee that skill on the confirmation turn. A ProposalHint
/// carries no invocation parameters and must therefore never be surfaced to the model as a
/// redeemable token — the two purposes are read by two independent paths that never overlap.
/// CorrectionUndo is the undo offer of a graceful correction (spec §1 rule 3). It is a gate-replay row
/// in every respect but its lifetime: the offer is made ONCE, inside the correction answer and never as
/// a separate dialogue, so the token it leaves behind may only be redeemed by the turn that immediately
/// follows it. TurnPreparationService discards it on any message that does not affirm, which is what
/// keeps a later "ja" — to a question the model asked afterwards — from carrying out a write the user
/// never confirmed. A row of this purpose is otherwise indistinguishable to Consume, which keys on the
/// token alone, so confirm_pending_action replays it exactly like any other held invocation.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PendingConfirmationPurposes
{
    public const string GateReplay = "GateReplay";

    public const string ProposalHint = "ProposalHint";

    public const string CorrectionUndo = "CorrectionUndo";

    public const int MaxLength = 32;
}
