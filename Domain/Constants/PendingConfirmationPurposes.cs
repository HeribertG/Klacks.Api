// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Discriminator values for a row in the single pending-confirmation store. GateReplay is the
/// original purpose: the autonomy gate intercepted a real skill invocation, stored its parameters
/// and expects confirm_pending_action to replay them. ProposalHint is the pre-attempt case: a
/// read-only propose_* skill succeeded and merely records which apply_* skill the user may confirm
/// next, so the toolset assembler can guarantee that skill on the confirmation turn. A ProposalHint
/// carries no invocation parameters and must therefore never be surfaced to the model as a
/// redeemable token — the two purposes are read by two independent paths that never overlap.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PendingConfirmationPurposes
{
    public const string GateReplay = "GateReplay";

    public const string ProposalHint = "ProposalHint";

    public const int MaxLength = 32;
}
