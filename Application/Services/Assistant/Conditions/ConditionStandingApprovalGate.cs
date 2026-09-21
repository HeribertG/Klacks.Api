// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The approval-free path of the action dispatcher (Owner decisions 2026-09-21): decides whether one
/// candidate row may be remediated under an administrator's standing approval instead of under an answer
/// to this very finding, and prepares everything that decision needs. Lives in its own class rather than
/// in AgentConditionActionService for the same two reasons ConditionActionBudget does - that class is
/// against its size-guard ceiling, and this is one cohesive rule with its own failure modes.
///
/// It is reached only after every gate of the dispatcher has already passed (effective MaxAction at
/// Execute, cascade guard, attempt limit, quiet window, per-tick cap, governance daily budget and circuit
/// breaker) and only for a row that carries no approval of its own. It relaxes none of them: the grant's
/// own daily budget is a SECOND, tighter ceiling measured in the same claim count, and the granting
/// administrator's account, roles, the skill's permissions and the unattended policy are re-checked here,
/// at execution time, not at grant time.
///
/// ORDER IS THE SAFETY. The identity is resolved BEFORE the approval is stamped onto the row. Stamping
/// first and discovering afterwards that the granter can no longer act would withdraw the stamp again on
/// every single tick - an unbounded stamp-and-withdraw loop that never counts an attempt, because the
/// withdrawal happens before any claim is made and therefore never carries the row towards escalation.
/// </summary>
/// <param name="repository">Active grant per kind and exact scope.</param>
/// <param name="identityProvider">Mints and re-checks the granting administrator's identity.</param>
/// <param name="ledgerService">Writes the approval stamp the executing path then reads.</param>
/// <param name="logger">Records every grant that did not apply, and why - the counterpart of "no silent caps".</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionStandingApprovalGate
{
    private const string BudgetSpentMessage =
        "Standing approval {StandingApprovalId} covers condition {ConditionId} ({Kind}) but its daily budget "
        + "of {DailyBudget} is used up for the company day; the finding falls back to the approval chain";

    private const string UnusableMessage =
        "Standing approval {StandingApprovalId} covers condition {ConditionId} but its granting user "
        + "{GrantedByUserId} cannot act right now, so the grant is treated as invalid for this finding and it "
        + "falls back to the approval chain: {Reason}";

    private const string StampLostMessage =
        "Condition {ConditionId} was approved by somebody else while standing approval {StandingApprovalId} "
        + "was being applied; the row keeps that approval and a later tick executes it";

    private readonly IStandingApprovalRepository _repository;
    private readonly IProactiveActionIdentityProvider _identityProvider;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly ILogger _logger;

    public ConditionStandingApprovalGate(
        IStandingApprovalRepository repository,
        IProactiveActionIdentityProvider identityProvider,
        IAgentConditionLedgerService ledgerService,
        ILogger logger)
    {
        _repository = repository;
        _identityProvider = identityProvider;
        _ledgerService = ledgerService;
        _logger = logger;
    }

    /// <summary>
    /// Applies a standing approval to this row if one covers it. Falls back to the approval chain - which
    /// is strictly more conservative than executing, because it asks a human - for every way the grant can
    /// fail to cover the row: the row is not Reported (a claim made by a regime that left no stamp must not
    /// be resumed under a grant that may have been given afterwards); no grant matches the kind and the
    /// row's EXACT GroupId, or it is expired or revoked; the grant's daily budget is spent; or the granting
    /// administrator can no longer act.
    /// </summary>
    /// <param name="condition">The candidate row, past all of the dispatcher's own gates.</param>
    /// <param name="entry">Its kind's remediation, whose skill the permissions are checked against.</param>
    /// <param name="budget">The tick's budget for this kind, which also counts the claims made in it.</param>
    /// <param name="nowUtc">The tick's single "now"; expiry is judged against it.</param>
    public async Task<ConditionStandingApprovalDecision> TryApplyAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        ConditionActionBudget budget,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (condition.Status != AgentConditionStatus.Reported)
        {
            return ConditionStandingApprovalDecision.NotCovered;
        }

        var standing = await _repository.FindActiveAsync(
            condition.TriggerKind, condition.GroupId, nowUtc, cancellationToken);
        if (standing is null)
        {
            return ConditionStandingApprovalDecision.NotCovered;
        }

        if (!await budget.WithinStandingApprovalBudgetAsync(
            condition.GroupId, standing.DailyBudget, cancellationToken))
        {
            _logger.LogInformation(
                BudgetSpentMessage, standing.Id, condition.Id, condition.TriggerKind, standing.DailyBudget);
            return ConditionStandingApprovalDecision.NotCovered;
        }

        var identity = await _identityProvider.ResolveForSkillAsync(
            standing.GrantedByUserId, condition.Id, entry.RemediationSkillName, cancellationToken);
        if (!identity.Success || identity.Context is null)
        {
            _logger.LogWarning(
                UnusableMessage,
                standing.Id, condition.Id, standing.GrantedByUserId, identity.Reason ?? string.Empty);
            return ConditionStandingApprovalDecision.NotCovered;
        }

        if (!await _ledgerService.TryApproveAsync(condition.Id, standing.GrantedByUserId, cancellationToken))
        {
            _logger.LogInformation(StampLostMessage, condition.Id, standing.Id);
            return ConditionStandingApprovalDecision.LeaveAlone;
        }

        return ConditionStandingApprovalDecision.Execute(
            new ConditionExecutionAuthority(standing.GrantedByUserId, identity, standing));
    }
}
