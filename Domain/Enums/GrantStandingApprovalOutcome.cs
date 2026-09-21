// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum GrantStandingApprovalOutcome
{
    /// <summary>The grant was stored and applies from now until its expiry.</summary>
    Granted = 0,

    /// <summary>A grant for the same kind and scope is still active; it has to be revoked first.</summary>
    AlreadyActive = 1,

    /// <summary>The grant was refused - unknown or unremediable kind, an irreversible remediation, or a value outside the hard limits.</summary>
    Refused = 2
}
