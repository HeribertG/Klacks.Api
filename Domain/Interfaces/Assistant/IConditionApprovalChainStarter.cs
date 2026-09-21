// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Opens the approval chain for one executable condition: resolves who may approve the remediation,
/// derives the deadline and starts the ProactiveApproval chain. Owns the two "not now" rules the tick
/// must never get wrong - one Running chain per condition, and no restart on the company day a previous
/// chain ended on - so the action dispatcher only has to ask and count the answer.
/// </summary>
/// <param name="condition">The Reported ledger row whose effective max action is Execute and which carries no approval yet.</param>
/// <param name="entry">The registry entry naming the remediation skill whose RequiredPermissions filter the roster.</param>
/// <param name="companyDayStartUtc">The UTC instant the current company day began; a chain created at or after it blocks a restart.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConditionApprovalChainStarter
{
    Task<ConditionApprovalStartOutcome> TryStartAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        DateTime companyDayStartUtc,
        CancellationToken cancellationToken = default);
}
