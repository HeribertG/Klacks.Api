// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Claim BEFORE act: takes one candidate row for this run through the ledger's compare-and-swap, which
/// raises AttemptCount in the same UPDATE. A lost claim means SKIP THIS ROW.
/// </summary>
/// <param name="ledgerService">Ledger writes: the Reported-to-Prepared claim and the stale-claim reclaim.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionActionClaimer
{
    private const string ClaimDetailFormat = "{0}skill={1} attempt={2}";
    private const string StandingApprovalClaimSuffixFormat = " standing-approval={0}";

    private readonly IAgentConditionLedgerService _ledgerService;

    public ConditionActionClaimer(IAgentConditionLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    /// <summary>
    /// Takes the row for this run. A Reported row is moved to Prepared; a Prepared row is a claim
    /// somebody else made, and is taken over only when it has gone stale - both raise AttemptCount and
    /// stamp LastAttemptAtUtc inside the same conditional UPDATE. The claim event names the approver as
    /// UserId, so the audit trail shows on whose authority the row was taken.
    ///
    /// A claim made under a standing approval carries that grant's id in the same detail string. It has to
    /// stay a SUFFIX: the budget and the circuit breaker recognise a budget-consuming event by the
    /// AgentConditionActionDefaults.ActionClaimDetailPrefix at the START of Detail, so anything appended
    /// is invisible to them while a changed prefix would silently stop the claim from counting.
    /// </summary>
    public async Task<bool> TryClaimAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        Guid approverUserId,
        Guid? standingApprovalId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var detail = string.Format(
            CultureInfo.InvariantCulture,
            ClaimDetailFormat,
            AgentConditionActionDefaults.ActionClaimDetailPrefix,
            entry.RemediationSkillName,
            condition.AttemptCount + 1);

        if (standingApprovalId is Guid grantId)
        {
            detail += string.Format(
                CultureInfo.InvariantCulture, StandingApprovalClaimSuffixFormat, grantId);
        }

        if (condition.Status == AgentConditionStatus.Reported)
        {
            return await _ledgerService.TryTransitionAsync(
                condition.Id,
                AgentConditionStatus.Reported,
                AgentConditionStatus.Prepared,
                userId: approverUserId,
                detail: detail,
                fields: new AgentConditionTransitionFields(
                    LastAttemptAtUtc: nowUtc,
                    AttemptIncrement: 1),
                cancellationToken);
        }

        return await _ledgerService.TryReclaimStaleAsync(
            condition.Id,
            TimeSpan.FromMinutes(AgentConditionActionDefaults.StaleClaimMinutes),
            detail,
            cancellationToken);
    }
}
