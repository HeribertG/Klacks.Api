// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IAgentConditionActionService"/> - the Etappe 5b action dispatcher, the first place
/// where Klacksy carries a change out on its own. It runs over the LEDGER, not over the tick's findings,
/// so a finding reported in one tick can be acted on in a later one and a crashed run resumes instead of
/// being lost.
///
/// Order of the gates per condition, each of which exists for a different failure it prevents:
/// (0) the effective MaxAction - governance folded with the Etappe-4e delegation and then capped by the
///     code-only remediation registry - decides whether this row is in the action branch at all;
/// (1) the cascade guard never auto-handles a row an earlier Klacksy execution may have produced;
/// (2) a row that has already been attempted MaxAttemptsBeforeEscalation times is escalated to a human
///     rather than retried into a loop;
/// (3) a quiet window skips WITHOUT counting an attempt, so a long import cannot starve the escalation;
/// (4) an absolute per-kind-per-tick cap that no governance value can widen - checked before the budget
///     because it costs no query;
/// (5) the daily budget and the circuit breaker, counted in the database from the claims' own audit
///     events so several API instances share one budget instead of one each, and counted PER GROUP.
///
/// Since the approval chain (design 2026-09-20, Owner decisions final) "Execute" means EXECUTE AFTER
/// APPROVAL. A Reported row that passes the gates and carries no approval gets an approval chain asked
/// for it (IConditionApprovalChainStarter) and nothing else; the acknowledging candidate - or the planner
/// who delegated the condition - is stamped onto the row as its approver, and a LATER tick executes the
/// stamped row under the approver's own rights - within ApprovalExecutionWindowMinutes of the stamp, with
/// the approver's account, roles and the skill's permissions re-checked at that moment. Nobody
/// acknowledges: the chain exhausts, nothing runs, no retry before the next company day. There is no
/// unattended stage and no stored owner: nothing executes without a human's stamp on the row.
///
/// Since the standing approval (Owner decisions 2026-09-21) that stamp may also come from an
/// administrator's ADVANCE consent instead of from an answer to this finding: when a grant covers the
/// kind and the row's exact scope, has not expired or been revoked and still has budget for the company
/// day, the dispatcher stamps the granting administrator onto the row itself and executes in the same
/// tick - so no approval chain is ever started for such a row. Nothing else is relaxed. Every gate above
/// runs first and unchanged, the grant's own daily budget is a SECOND, tighter ceiling on the same claim
/// count, and the granting administrator's account, roles and the skill's permissions are re-checked at
/// execution time BEFORE the stamp is written, so a grant from somebody who has since lost the rights
/// leaves no stamp behind and the finding falls back to the normal chain.
///
/// Claim BEFORE act, always. The claim is a compare-and-swap that raises AttemptCount in the same
/// UPDATE, so a run that dies between claim and outcome still counts as an attempt and the row escalates
/// instead of retrying forever. A lost claim means SKIP THIS ROW - never "the budget was not consumed".
///
/// The gates and the acting end live in collaborators the constructor composes - ConditionCandidatePipeline
/// (gate order), ConditionActionGovernance, ConditionCascadeGuard, ConditionApprovalHandler,
/// ConditionRemediationExecutor, ConditionActionClaimer and ConditionActionOutcomeRecorder - all sharing this
/// class's logger; this class owns the tick: one "now", the company day, and one ConditionActionKindRun per kind.
/// </summary>
/// <param name="repository">Ledger reads: candidates, budget counts, recent executions for the cascade guard.</param>
/// <param name="ledgerService">Ledger writes: claims, reclaims, transitions, approvals, attempt and provenance events.</param>
/// <param name="governanceResolver">Per-kind, per-scope MaxAction and budget values (Etappe 4a).</param>
/// <param name="registry">Code-only map from kind to remediation; absence caps the kind at Hint.</param>
/// <param name="quietWindow">Answers whether now is a bad moment to touch this condition's target.</param>
/// <param name="identityProvider">Borrows the approver's rights under Klacksy's own name.</param>
/// <param name="skillExecutor">Runs the remediation skill.</param>
/// <param name="reporter">Mandatory post-action report, never subject to the notification rate limit.</param>
/// <param name="approvalStarter">Opens the approval chain for an executable row that nobody has approved yet.</param>
/// <param name="standingApprovals">An administrator's advance approval for the kind and scope, which replaces that chain while it lasts.</param>
/// <param name="timeProvider">Clock, injected so the approval and stale-claim windows are testable.</param>
/// <param name="companyClock">Resolves the company's time zone, so the daily action budget resets at the company's midnight rather than the UTC calendar day's.</param>
/// <param name="logger">Structured log per kind and per skipped row - the counterpart of "no silent caps".</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class AgentConditionActionService : IAgentConditionActionService
{
    private const string KindFailedMessage = "Proactive action dispatcher failed for kind {Kind}";

    private const string TickSummaryMessage =
        "Proactive action tick: {Considered} considered, {Executed} executed, {Failed} failed, "
        + "{Escalated} escalated, {SkippedCascade} cascade, {SkippedQuiet} quiet, "
        + "{SkippedUnbindable} unbindable, {SkippedNoApprover} claimed without approver, {SkippedClaimLost} claim lost, "
        + "{LeftForBudget} left for budget, {ApprovalsRequested} approvals requested, "
        + "{AwaitingApproval} awaiting approval, {ApprovalsUnavailable} approvals unavailable, "
        + "{ApprovalsWithdrawn} approvals withdrawn";

    private static readonly AgentConditionActionTickResult EmptyResult = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private readonly IAgentConditionRepository _repository;
    private readonly IConditionRemediationRegistry _registry;
    private readonly TimeProvider _timeProvider;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<AgentConditionActionService> _logger;
    private readonly ConditionCandidatePipeline _pipeline;

    public AgentConditionActionService(
        IAgentConditionRepository repository,
        IAgentConditionLedgerService ledgerService,
        IProactiveGovernanceResolver governanceResolver,
        IConditionRemediationRegistry registry,
        IQuietWindowService quietWindow,
        IProactiveActionIdentityProvider identityProvider,
        ISkillExecutor skillExecutor,
        IProactiveActionReporter reporter,
        IConditionApprovalChainStarter approvalStarter,
        IStandingApprovalRepository standingApprovals,
        TimeProvider timeProvider,
        ICompanyClock companyClock,
        ILogger<AgentConditionActionService> logger)
    {
        _repository = repository;
        _registry = registry;
        _timeProvider = timeProvider;
        _companyClock = companyClock;
        _logger = logger;

        var argumentBinder = new ConditionRemediationArgumentBinder(repository, logger);
        var outcomeRecorder = new ConditionActionOutcomeRecorder(ledgerService, reporter, logger);
        var approvalHandler = new ConditionApprovalHandler(approvalStarter, ledgerService, outcomeRecorder, logger);
        var executor = new ConditionRemediationExecutor(
            identityProvider, skillExecutor, new ConditionActionClaimer(ledgerService),
            argumentBinder, approvalHandler, outcomeRecorder, logger);

        _pipeline = new ConditionCandidatePipeline(
            new ConditionActionGovernance(governanceResolver, registry),
            new ConditionCascadeGuard(ledgerService, logger),
            quietWindow,
            argumentBinder,
            approvalHandler,
            outcomeRecorder,
            executor,
            new ConditionStandingApprovalGate(standingApprovals, identityProvider, ledgerService, logger),
            logger);
    }

    public async Task<AgentConditionActionTickResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var kinds = _registry.RegisteredKinds;
        if (kinds.Count == 0)
        {
            return EmptyResult;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var companyDayStartUtc = await ResolveCompanyDayStartUtcAsync(nowUtc, cancellationToken);
        var tally = new ConditionActionTally();
        var recentExecutions = await _repository.GetExecutedSinceAsync(
            nowUtc.AddMinutes(-AgentConditionActionDefaults.CascadeWindowMinutes), cancellationToken);

        foreach (var triggerKind in kinds)
        {
            try
            {
                await RunKindAsync(triggerKind, nowUtc, companyDayStartUtc, recentExecutions, tally, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, KindFailedMessage, triggerKind);
            }
        }

        var result = tally.ToResult();
        LogTickSummary(result);

        return result;
    }

    /// <summary>
    /// The UTC instant the company's own calendar day began "today" - NOT the UTC calendar day's own
    /// midnight. The daily action budget and the "no second chain today" rule are both measured against
    /// it. Derives "today" from the same <paramref name="nowUtc"/> snapshot the rest of the tick uses -
    /// ICompanyClock is consulted only for the time zone - so a single tick can never straddle two
    /// different "now" reads across the DST/UTC-offset conversion and the rest of its own logic.
    /// </summary>
    private async Task<DateTime> ResolveCompanyDayStartUtcAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var zone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone));
        return CompanyWallClockToUtcConverter.ConvertToUtc(localToday.ToDateTime(TimeOnly.MinValue), zone);
    }

    /// <summary>
    /// Walks one kind's candidates with a fresh ConditionActionKindRun, so the governance cache, the
    /// budget and the per-tick cap never carry over from one kind to the next.
    /// </summary>
    private async Task RunKindAsync(
        string triggerKind,
        DateTime nowUtc,
        DateTime companyDayStartUtc,
        IReadOnlyList<AgentCondition> recentExecutions,
        ConditionActionTally tally,
        CancellationToken cancellationToken)
    {
        if (!_registry.TryGetEntry(triggerKind, out var entry) || entry is null)
        {
            return;
        }

        var candidates = await _repository.GetActionableByKindAsync(
            triggerKind, AgentConditionActionDefaults.CandidateQueryCap, cancellationToken);
        if (candidates.Count == 0)
        {
            return;
        }

        var run = new ConditionActionKindRun(
            triggerKind,
            entry,
            nowUtc,
            companyDayStartUtc,
            recentExecutions,
            candidates,
            new ConditionActionBudget(_repository, triggerKind, nowUtc, companyDayStartUtc),
            tally);

        for (var index = 0; index < candidates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tally.Considered++;

            if (await _pipeline.ProcessAsync(run, index, cancellationToken))
            {
                return;
            }
        }
    }

    private void LogTickSummary(AgentConditionActionTickResult result) =>
        _logger.LogInformation(
            TickSummaryMessage,
            result.Considered, result.Executed, result.Failed, result.Escalated, result.SkippedCascade,
            result.SkippedQuiet, result.SkippedUnbindable, result.SkippedNoApprover, result.SkippedClaimLost,
            result.LeftForBudget, result.ApprovalsRequested, result.AwaitingApproval, result.ApprovalsUnavailable,
            result.ApprovalsWithdrawn);
}
