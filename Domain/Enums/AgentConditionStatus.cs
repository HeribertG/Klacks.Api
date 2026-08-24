// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Lifecycle state of a condition-ledger row. Legal transitions:
/// Detected -> Reported -> Prepared -> Executed | Rejected | Resolved | Escalated. Every transition is
/// a compare-and-swap update (WHERE status = expected) so two API instances racing the same tick can
/// never both win. Executed, Rejected, Resolved and Escalated are terminal: once reached, a re-arm
/// (the same fingerprint detected again) inserts a brand-new row rather than reopening this one, so the
/// partial unique index on Fingerprint only ever excludes these four values.
/// </summary>
public enum AgentConditionStatus
{
    Detected = 0,
    Reported = 1,
    Prepared = 2,
    Executed = 3,
    Rejected = 4,
    Resolved = 5,
    Escalated = 6
}
