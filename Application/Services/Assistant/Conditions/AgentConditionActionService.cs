// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
/// Claim BEFORE act, always. The claim is a compare-and-swap that raises AttemptCount in the same
/// UPDATE, so a run that dies between claim and outcome still counts as an attempt and the row escalates
/// instead of retrying forever. A lost claim means SKIP THIS ROW - never "the budget was not consumed".
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
/// <param name="timeProvider">Clock, injected so the approval and stale-claim windows are testable.</param>
/// <param name="companyClock">Resolves the company's time zone, so the daily action budget resets at the company's midnight rather than the UTC calendar day's.</param>
/// <param name="logger">Structured log per kind and per skipped row - the counterpart of "no silent caps".</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class AgentConditionActionService : IAgentConditionActionService
{
    private const string ClaimDetailFormat = "{0}skill={1} attempt={2}";
    private const string OutcomeDetailFormat = "{0}{1}";
    private const string ExecutedDetail = "executed {0}";
    private const string FailedDetail = "attempt failed: {0}";
    private const string EscalatedDetail = "ineffective after {0} attempt(s)";
    private const string ApprovalExpiredDetail = "approval older than {0} minute(s) when the tick reached the row";
    private const string ApprovalRefusedDetail = "approver no longer qualifies: {0}";

    private const string ExecutedReportFormat =
        "I have carried out a remediation on my own.\n\n"
        + "Finding: {0} (condition {1})\nAction: {2}\nResult: {3}";

    private const string FailedReportFormat =
        "A remediation I attempted on my own did not work.\n\n"
        + "Finding: {0} (condition {1})\nAction: {2}\nProblem: {3}\n"
        + "Attempt {4} of {5}; after that I stop trying and leave it to you.";

    private const string EscalatedReportFormat =
        "I am giving up on a finding and leaving it to you.\n\n"
        + "Finding: {0} (condition {1})\nI attempted the remediation {2} time(s) without success.";

    private const string BudgetReportFormat =
        "I stopped acting on '{0}' for now: {1}.\n"
        + "{2} further finding(s) of this kind stay open and unhandled until the limit frees up again.";

    private const string ApprovalWithdrawnReportFormat =
        "An approved remediation was NOT carried out.\n\n"
        + "Finding: {0} (condition {1})\nAction: {2}\nReason: {3}\n"
        + "The finding stays open; I will ask for approval again on the next company day at the earliest.";

    private const string TickCapReason =
        "the absolute cap of {0} action(s) per kind per scan is reached";

    private const string PrepareWithoutScenarioMessage =
        "Kind {Kind} is governed at Prepare, but its remediation {Skill} is execute-only and cannot be "
        + "staged as a scenario; condition {ConditionId} is reported and left to a human";

    private const string NoResultMessage = "no message";

    private const string UnbindableAfterClaimReason =
        "the condition's payload changed after the claim and no longer supplies the remediation's arguments";

    private const string UnbindableAfterClaimMessage =
        "Condition {ConditionId} no longer binds {Skill} after the claim, so nothing was executed; the row "
        + "stays claimed and is left to the stale-claim reclaim";

    private const string NoApproverForClaimedRowMessage =
        "Condition {ConditionId} of kind {Kind} is claimed but carries no approval, so it is left to escalate";

    private static readonly AgentConditionActionTickResult EmptyResult = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private readonly IAgentConditionRepository _repository;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IProactiveGovernanceResolver _governanceResolver;
    private readonly IConditionRemediationRegistry _registry;
    private readonly IQuietWindowService _quietWindow;
    private readonly IProactiveActionIdentityProvider _identityProvider;
    private readonly ISkillExecutor _skillExecutor;
    private readonly IProactiveActionReporter _reporter;
    private readonly IConditionApprovalChainStarter _approvalStarter;
    private readonly TimeProvider _timeProvider;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<AgentConditionActionService> _logger;
    private readonly ConditionRemediationArgumentBinder _argumentBinder;

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
        TimeProvider timeProvider,
        ICompanyClock companyClock,
        ILogger<AgentConditionActionService> logger)
    {
        _repository = repository;
        _ledgerService = ledgerService;
        _governanceResolver = governanceResolver;
        _registry = registry;
        _quietWindow = quietWindow;
        _identityProvider = identityProvider;
        _skillExecutor = skillExecutor;
        _reporter = reporter;
        _approvalStarter = approvalStarter;
        _timeProvider = timeProvider;
        _companyClock = companyClock;
        _logger = logger;
        _argumentBinder = new ConditionRemediationArgumentBinder(repository, logger);
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
                _logger.LogError(ex, "Proactive action dispatcher failed for kind {Kind}", triggerKind);
            }
        }

        var result = tally.ToResult();
        _logger.LogInformation(
            "Proactive action tick: {Considered} considered, {Executed} executed, {Failed} failed, "
            + "{Escalated} escalated, {SkippedCascade} cascade, {SkippedQuiet} quiet, "
            + "{SkippedUnbindable} unbindable, {SkippedNoApprover} claimed without approver, {SkippedClaimLost} claim lost, "
            + "{LeftForBudget} left for budget, {ApprovalsRequested} approvals requested, "
            + "{AwaitingApproval} awaiting approval, {ApprovalsUnavailable} approvals unavailable, "
            + "{ApprovalsWithdrawn} approvals withdrawn",
            result.Considered, result.Executed, result.Failed, result.Escalated, result.SkippedCascade,
            result.SkippedQuiet, result.SkippedUnbindable, result.SkippedNoApprover, result.SkippedClaimLost,
            result.LeftForBudget, result.ApprovalsRequested, result.AwaitingApproval, result.ApprovalsUnavailable,
            result.ApprovalsWithdrawn);

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

        var run = new KindRun(
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

            if (await ProcessCandidateAsync(run, index, cancellationToken))
            {
                return;
            }
        }
    }

    /// <summary>
    /// One candidate through the gates. Returns true when the kind must stop for this tick (the absolute
    /// per-tick cap), false to walk on to the next row - including after a group's budget block, because
    /// the candidates behind it may belong to groups that have spent nothing.
    /// </summary>
    private async Task<bool> ProcessCandidateAsync(KindRun run, int index, CancellationToken cancellationToken)
    {
        var condition = run.Candidates[index];
        var governance = await ResolveGovernanceAsync(run.GovernanceCache, condition, cancellationToken);
        var maxAction = EffectiveMaxActionFor(governance, condition);
        if (maxAction < ProactiveMaxAction.Execute)
        {
            if (maxAction == ProactiveMaxAction.Prepare && !run.Entry.IsScenarioCapable)
            {
                _logger.LogDebug(
                    PrepareWithoutScenarioMessage, run.TriggerKind, run.Entry.RemediationSkillName, condition.Id);
            }

            return false;
        }

        if (await IsCascadeAsync(condition, run.RecentExecutions, cancellationToken))
        {
            run.Tally.SkippedCascade++;
            return false;
        }

        var approver = ResolveApprover(condition);

        if (condition.AttemptCount >= AgentConditionActionDefaults.MaxAttemptsBeforeEscalation)
        {
            if (await EscalateAsync(condition, approver, cancellationToken))
            {
                run.Tally.Escalated++;
            }

            return false;
        }

        if (await _quietWindow.IsQuietForAsync(condition, cancellationToken))
        {
            run.Tally.SkippedQuiet++;
            return false;
        }

        var arguments = _argumentBinder.TryBind(run.Entry, condition);
        if (arguments is null)
        {
            run.Tally.SkippedUnbindable++;
            return false;
        }

        if (approver is Guid freshApprover
            && condition.Status == AgentConditionStatus.Reported
            && await WithdrawIfApprovalIsStaleAsync(run, condition, freshApprover, cancellationToken))
        {
            return false;
        }

        if (run.ActionsThisTick >= AgentConditionActionDefaults.MaxExecutionsPerKindPerTick)
        {
            var remaining = run.Candidates.Count - index;
            await ReportBudgetStopAsync(
                run, condition, approver,
                string.Format(CultureInfo.InvariantCulture, TickCapReason, AgentConditionActionDefaults.MaxExecutionsPerKindPerTick),
                remaining, cancellationToken);
            run.Tally.LeftForBudget += remaining;
            return true;
        }

        var blockedReason = await run.Budget.DescribeBlockAsync(condition.GroupId, governance, cancellationToken);
        if (blockedReason is not null)
        {
            if (run.Budget.TryMarkStopReported(condition.GroupId))
            {
                var leftInGroup = run.Candidates.Skip(index).Count(remaining => remaining.GroupId == condition.GroupId);
                await ReportBudgetStopAsync(run, condition, approver, blockedReason, leftInGroup, cancellationToken);
                run.Tally.LeftForBudget += leftInGroup;
            }

            return false;
        }

        if (approver is not Guid approverUserId)
        {
            await AskForApprovalOrSkipAsync(run, condition, cancellationToken);
            return false;
        }

        await ClaimAndExecuteAsync(run, condition, approverUserId, arguments, cancellationToken);
        return false;
    }

    /// <summary>
    /// Whose rights this row would run under: the stamped approver - the approval chain's or the
    /// delegation's answer - or null when nobody has released it yet.
    /// </summary>
    private static Guid? ResolveApprover(AgentCondition condition) =>
        condition.ApprovedByUserId is Guid approverUserId && approverUserId != Guid.Empty ? approverUserId : null;

    /// <summary>
    /// A Reported row with nobody behind it gets its approval chain asked for; a Prepared row with nobody
    /// behind it is a claim this regime cannot resume (no approval to run under) and is left to age into
    /// escalation. Every non-Started outcome of the starter is fail closed: counted, logged by the starter
    /// itself, and never retried within this tick. A started chain counts against the per-tick cap so one
    /// busy kind cannot flood a planner's inbox with requests in a single scan.
    /// </summary>
    private async Task AskForApprovalOrSkipAsync(KindRun run, AgentCondition condition, CancellationToken cancellationToken)
    {
        if (condition.Status != AgentConditionStatus.Reported)
        {
            _logger.LogDebug(NoApproverForClaimedRowMessage, condition.Id, condition.TriggerKind);
            run.Tally.SkippedNoApprover++;
            return;
        }

        var outcome = await _approvalStarter.TryStartAsync(condition, run.Entry, run.CompanyDayStartUtc, cancellationToken);
        switch (outcome)
        {
            case ConditionApprovalStartOutcome.Started:
                run.Tally.ApprovalsRequested++;
                run.ActionsThisTick++;
                break;
            case ConditionApprovalStartOutcome.ChainAlreadyRunning:
            case ConditionApprovalStartOutcome.WaitingForNextCompanyDay:
                run.Tally.AwaitingApproval++;
                break;
            default:
                run.Tally.ApprovalsUnavailable++;
                break;
        }
    }

    /// <summary>
    /// The execution window of a fresh approval: a stamp older than ApprovalExecutionWindowMinutes when
    /// the tick reaches the row is withdrawn rather than acted on, because the finding may no longer be
    /// what the approver looked at. The window is measured in scan intervals, not in the stale-claim
    /// minutes: an approval is answered by a LATER tick, so it must survive at least the one tick that may
    /// legitimately pass the row over. Returns true when the row was withdrawn (or the withdrawal was lost
    /// to a concurrent tick), in which case the caller must not act on it.
    /// </summary>
    private async Task<bool> WithdrawIfApprovalIsStaleAsync(
        KindRun run, AgentCondition condition, Guid approverUserId, CancellationToken cancellationToken)
    {
        var freshAfterUtc = run.NowUtc.AddMinutes(-AgentConditionActionDefaults.ApprovalExecutionWindowMinutes);
        if (condition.ApprovedAtUtc is { } approvedAtUtc && approvedAtUtc >= freshAfterUtc)
        {
            return false;
        }

        await WithdrawApprovalAsync(
            run, condition, approverUserId,
            string.Format(
                CultureInfo.InvariantCulture, ApprovalExpiredDetail, AgentConditionActionDefaults.ApprovalExecutionWindowMinutes),
            cancellationToken);
        return true;
    }

    private async Task WithdrawApprovalAsync(
        KindRun run, AgentCondition condition, Guid approverUserId, string reason, CancellationToken cancellationToken)
    {
        var withdrawn = await _ledgerService.TryWithdrawApprovalAsync(condition.Id, approverUserId, reason, cancellationToken);
        if (!withdrawn)
        {
            return;
        }

        run.Tally.ApprovalsWithdrawn++;
        await ReportAsync(
            condition, approverUserId,
            string.Format(
                CultureInfo.InvariantCulture, ApprovalWithdrawnReportFormat,
                condition.TriggerKind, condition.Id, run.Entry.RemediationSkillName, reason),
            cancellationToken);
    }

    /// <summary>
    /// Claim, re-bind, execute. On a fresh approval the identity is resolved BEFORE the claim so an
    /// approver who lost their account, role or the skill's permission since acknowledging costs no
    /// attempt: the approval is withdrawn, reported, and the finding waits for a new chain. On a resumed
    /// claim (Prepared) the identity is resolved after the claim, so a refusal there consumes an attempt
    /// and the row escalates instead of spinning.
    /// </summary>
    private async Task ClaimAndExecuteAsync(
        KindRun run,
        AgentCondition condition,
        Guid approverUserId,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        ProactiveActionIdentity? identity = null;
        if (condition.Status == AgentConditionStatus.Reported)
        {
            identity = await _identityProvider.ResolveForSkillAsync(
                approverUserId, condition.Id, run.Entry.RemediationSkillName, cancellationToken);
            if (!identity.Success || identity.Context is null)
            {
                await WithdrawApprovalAsync(
                    run, condition, approverUserId,
                    string.Format(CultureInfo.InvariantCulture, ApprovalRefusedDetail, identity.Reason ?? string.Empty),
                    cancellationToken);
                return;
            }
        }

        if (!await TryClaimAsync(condition, run.Entry, approverUserId, run.NowUtc, cancellationToken))
        {
            run.Tally.SkippedClaimLost++;
            return;
        }

        run.Budget.RecordClaim(condition.GroupId);
        run.ActionsThisTick++;

        var claimedArguments = await _argumentBinder.RebindAfterClaimAsync(run.Entry, condition, arguments, cancellationToken);
        if (claimedArguments is null)
        {
            _logger.LogWarning(UnbindableAfterClaimMessage, condition.Id, run.Entry.RemediationSkillName);
            await RecordFailureAsync(condition, run.Entry, approverUserId, UnbindableAfterClaimReason, cancellationToken);
            run.Tally.Failed++;
            return;
        }

        if (await ExecuteAsync(condition, run.Entry, claimedArguments, approverUserId, identity, cancellationToken))
        {
            run.Tally.Executed++;
        }
        else
        {
            run.Tally.Failed++;
        }
    }

    /// <summary>
    /// Governance folded with the Etappe-4e delegation, re-capped by the global autonomy level and then
    /// capped by the remediation registry. Precedence is not negotiable in three places: the global kill
    /// switch and a disabled kind pin the result at Hint BEFORE the delegation is looked at, because a
    /// human's earlier "you handle this one" grant must never survive the emergency stop; the global
    /// autonomy level caps the delegation too (Owner decision 2026-08-28); and the registry cap applies
    /// LAST, so no delegation can steer a kind that has no remediation past Hint.
    /// </summary>
    private ProactiveMaxAction EffectiveMaxActionFor(ProactiveGovernanceDecision governance, AgentCondition condition)
    {
        if (governance.KillSwitchActive || !governance.Enabled)
        {
            return ProactiveMaxAction.Hint;
        }

        var requested = condition.DelegatedMaxAction is { } delegated && delegated > governance.EffectiveMaxAction
            ? delegated
            : governance.EffectiveMaxAction;

        var levelCapped = requested < governance.GlobalAutonomyCap ? requested : governance.GlobalAutonomyCap;

        return _registry.TryGetEffectiveMaxAction(condition.TriggerKind, levelCapped);
    }

    private async Task<ProactiveGovernanceDecision> ResolveGovernanceAsync(
        ConditionGovernanceCache cache,
        AgentCondition condition,
        CancellationToken cancellationToken)
    {
        if (cache.TryGet(condition.GroupId, out var cached))
        {
            return cached!;
        }

        var decision = await _governanceResolver.ResolveAsync(
            condition.TriggerKind, condition.GroupId, cancellationToken);
        cache.Set(condition.GroupId, decision);

        return decision;
    }

    /// <summary>
    /// True when this row must never be auto-handled because Klacksy itself may have produced it: either
    /// it already carries a provenance link, or it was detected after a Klacksy execution on the same
    /// entity within one scan interval - in which case the link is written now, so the attribution
    /// survives this tick. Matching is on EntityId when the candidate has one and falls back to GroupId
    /// only when it does not: the cascade guard exists to catch "my fix broke the thing I touched", not
    /// "something happened nearby".
    /// </summary>
    private async Task<bool> IsCascadeAsync(
        AgentCondition condition,
        IReadOnlyList<AgentCondition> recentExecutions,
        CancellationToken cancellationToken)
    {
        if (condition.CausedByConditionId is not null)
        {
            return true;
        }

        var cause = FindCause(condition, recentExecutions);
        if (cause is not Guid causeId)
        {
            return false;
        }

        await _ledgerService.TrySetCausedByAsync(condition.Id, causeId, cancellationToken);
        _logger.LogInformation(
            "Condition {ConditionId} appeared after Klacksy executed condition {CauseId} on the same "
            + "target and is only ever hinted from now on",
            condition.Id, causeId);

        return true;
    }

    private static Guid? FindCause(AgentCondition condition, IReadOnlyList<AgentCondition> recentExecutions)
    {
        foreach (var executed in recentExecutions)
        {
            if (executed.Id == condition.Id || executed.HandledAtUtc is not { } handledAtUtc)
            {
                continue;
            }

            if (condition.DetectedAtUtc < handledAtUtc)
            {
                continue;
            }

            if (condition.EntityId is { } entityId)
            {
                if (executed.EntityId == entityId)
                {
                    return executed.Id;
                }

                continue;
            }

            if (condition.GroupId is { } groupId && executed.GroupId == groupId)
            {
                return executed.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Takes the row for this run. A Reported row is moved to Prepared; a Prepared row is a claim
    /// somebody else made, and is taken over only when it has gone stale - both raise AttemptCount and
    /// stamp LastAttemptAtUtc inside the same conditional UPDATE. The claim event names the approver as
    /// UserId, so the audit trail shows on whose authority the row was taken.
    /// </summary>
    private async Task<bool> TryClaimAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        Guid approverUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var detail = string.Format(
            CultureInfo.InvariantCulture,
            ClaimDetailFormat,
            AgentConditionActionDefaults.ActionClaimDetailPrefix,
            entry.RemediationSkillName,
            condition.AttemptCount + 1);

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

    private async Task<bool> ExecuteAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        IReadOnlyDictionary<string, object?> arguments,
        Guid approverUserId,
        ProactiveActionIdentity? preResolvedIdentity,
        CancellationToken cancellationToken)
    {
        var identity = preResolvedIdentity ?? await _identityProvider.ResolveForSkillAsync(
            approverUserId, condition.Id, entry.RemediationSkillName, cancellationToken);

        if (!identity.Success || identity.Context is null)
        {
            await RecordFailureAsync(condition, entry, approverUserId, identity.Reason ?? string.Empty, cancellationToken);
            return false;
        }

        SkillResult result;
        try
        {
            result = await _skillExecutor.ExecuteAsync(
                new SkillInvocation
                {
                    SkillName = entry.RemediationSkillName,
                    Parameters = arguments
                        .Where(argument => argument.Value is not null)
                        .ToDictionary(argument => argument.Key, argument => argument.Value!, StringComparer.Ordinal)
                },
                identity.Context,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Remediation {Skill} threw on condition {ConditionId}", entry.RemediationSkillName, condition.Id);
            await RecordFailureAsync(condition, entry, approverUserId, ex.Message, cancellationToken);
            return false;
        }

        var message = string.IsNullOrWhiteSpace(result.Message) ? NoResultMessage : result.Message!;
        if (!result.Success)
        {
            await RecordFailureAsync(condition, entry, approverUserId, message, cancellationToken);
            return false;
        }

        var recorded = await _ledgerService.TryTransitionAsync(
            condition.Id,
            AgentConditionStatus.Prepared,
            AgentConditionStatus.Executed,
            userId: approverUserId,
            detail: Outcome(string.Format(CultureInfo.InvariantCulture, ExecutedDetail, entry.RemediationSkillName)),
            fields: new AgentConditionTransitionFields(
                HandlingKind: AgentConditionHandlingKind.Executed,
                ApprovedByUserId: approverUserId),
            cancellationToken);

        if (!recorded)
        {
            _logger.LogWarning(
                "Remediation {Skill} on condition {ConditionId} succeeded but the row had already been "
                + "moved by somebody else; the execution is not recorded on the ledger",
                entry.RemediationSkillName, condition.Id);
        }

        await ReportAsync(
            condition, approverUserId,
            string.Format(
                CultureInfo.InvariantCulture, ExecutedReportFormat,
                condition.TriggerKind, condition.Id, entry.RemediationSkillName, message),
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Records a failed attempt WITHOUT moving the row: it stays Prepared so the stale-claim path can
    /// take it over on a later tick, and AttemptCount - already raised by the claim - carries it towards
    /// escalation. The report goes out for a failure too; a report only on success would let a
    /// remediation fail three times in silence before anybody hears about it.
    /// </summary>
    private async Task RecordFailureAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        Guid approverUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        var attempt = condition.AttemptCount + 1;

        await _ledgerService.RecordEventAsync(
            condition.Id,
            AgentConditionEventTypes.AttemptFailed,
            Outcome(string.Format(CultureInfo.InvariantCulture, FailedDetail, reason)),
            cancellationToken);

        await ReportAsync(
            condition, approverUserId,
            string.Format(
                CultureInfo.InvariantCulture, FailedReportFormat,
                condition.TriggerKind, condition.Id, entry.RemediationSkillName, reason,
                attempt, AgentConditionActionDefaults.MaxAttemptsBeforeEscalation),
            cancellationToken);
    }

    /// <summary>
    /// Hands one row to a human after MaxAttemptsBeforeEscalation attempts. Returns whether THIS caller
    /// escalated it: a lost compare-and-swap means another instance got there first, and counting it
    /// anyway would over-report the one number a planner reads to spot a stuck kind.
    /// </summary>
    private async Task<bool> EscalateAsync(AgentCondition condition, Guid? approverUserId, CancellationToken cancellationToken)
    {
        var escalated = await _ledgerService.TryTransitionAsync(
            condition.Id,
            condition.Status,
            AgentConditionStatus.Escalated,
            userId: approverUserId,
            detail: Outcome(string.Format(CultureInfo.InvariantCulture, EscalatedDetail, condition.AttemptCount)),
            cancellationToken: cancellationToken);

        if (!escalated)
        {
            return false;
        }

        await ReportAsync(
            condition, approverUserId,
            string.Format(
                CultureInfo.InvariantCulture, EscalatedReportFormat,
                condition.TriggerKind, condition.Id, condition.AttemptCount),
            cancellationToken);

        return true;
    }

    /// <summary>
    /// A budget stop is never silent: it is logged on EVERY tick it happens. The durable note is
    /// deliberately narrower - only on a tick that actually acted - because once a day's budget is spent
    /// every remaining tick of that day stops on its first candidate, and the tick on which the budget
    /// ran out is the one that carries the information.
    /// </summary>
    private async Task ReportBudgetStopAsync(
        KindRun run,
        AgentCondition condition,
        Guid? approverUserId,
        string reason,
        int remaining,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Proactive actions on {Kind} stopped: {Reason}. {Remaining} finding(s) stay open this tick",
            run.TriggerKind, reason, remaining);

        if (run.ActionsThisTick == 0)
        {
            return;
        }

        await ReportAsync(
            condition, approverUserId,
            string.Format(CultureInfo.InvariantCulture, BudgetReportFormat, run.TriggerKind, reason, remaining),
            cancellationToken);
    }

    /// <summary>
    /// Who hears about this row (Owner decision 2026-09-20): the approver plus the finding's planning
    /// audience (or the admins without a group); before any approval exists the audience alone.
    /// </summary>
    private Task ReportAsync(
        AgentCondition condition, Guid? approverUserId, string message, CancellationToken cancellationToken) =>
        _reporter.ReportToApprovalAudienceAsync(approverUserId, condition.GroupId, message, cancellationToken);

    private static string Outcome(string detail) =>
        string.Format(
            CultureInfo.InvariantCulture,
            OutcomeDetailFormat,
            AgentConditionActionDefaults.ActionOutcomeDetailPrefix,
            detail);

    private sealed class KindRun
    {
        public KindRun(
            string triggerKind,
            ConditionRemediationEntry entry,
            DateTime nowUtc,
            DateTime companyDayStartUtc,
            IReadOnlyList<AgentCondition> recentExecutions,
            IReadOnlyList<AgentCondition> candidates,
            ConditionActionBudget budget,
            ConditionActionTally tally)
        {
            TriggerKind = triggerKind;
            Entry = entry;
            NowUtc = nowUtc;
            CompanyDayStartUtc = companyDayStartUtc;
            RecentExecutions = recentExecutions;
            Candidates = candidates;
            Budget = budget;
            Tally = tally;
        }

        public string TriggerKind { get; }

        public ConditionRemediationEntry Entry { get; }

        public DateTime NowUtc { get; }

        public DateTime CompanyDayStartUtc { get; }

        public IReadOnlyList<AgentCondition> RecentExecutions { get; }

        public IReadOnlyList<AgentCondition> Candidates { get; }

        public ConditionActionBudget Budget { get; }

        public ConditionActionTally Tally { get; }

        public ConditionGovernanceCache GovernanceCache { get; } = new();

        public int ActionsThisTick { get; set; }
    }
}
