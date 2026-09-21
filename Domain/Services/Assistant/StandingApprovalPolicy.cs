// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What makes a standing approval apply. Kept here as ONE expression rather than as a predicate in the
/// repository plus a bool property on the entity: the two would be read by the grant validation, the
/// admin list and the action dispatcher, and a drift between them would mean a grant that the list shows
/// as expired while the dispatcher still executes under it. Written as an expression tree so the same
/// rule is translated into SQL by EF and evaluated in memory by a test.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class StandingApprovalPolicy
{
    /// <summary>
    /// A grant applies while it is neither revoked nor expired. Expiry is EXCLUSIVE - a tick at exactly
    /// ExpiresAtUtc no longer executes under the grant - so the stored instant is the first moment the
    /// autonomy window is closed again, which is what an administrator reads off a date.
    /// Soft-deleted rows never reach this predicate: every query goes through the entity's query filter.
    /// </summary>
    /// <param name="nowUtc">The caller's single "now"; a tick passes the same snapshot it uses elsewhere.</param>
    public static Expression<Func<StandingApproval, bool>> ActiveAt(DateTime nowUtc) =>
        approval => approval.RevokedAtUtc == null && approval.ExpiresAtUtc > nowUtc;
}
