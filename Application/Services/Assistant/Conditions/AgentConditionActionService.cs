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
/// (4) the daily budget and the circuit breaker, counted in the database from the claims' own audit
///     events so several API instances share one budget instead of one each;
/// (5) an absolute per-kind-per-tick cap that no governance value can widen.
///
/// Claim BEFORE act, always. The claim is a compare-and-swap that raises AttemptCount in the same
/// UPDATE, so a run that dies between claim and outcome still counts as an attempt and the row escalates
/// instead of retrying forever. A lost claim means SKIP THIS ROW - never "the budget was not consumed":
/// TryTransitionAsync can report false after a successful commit when the retrying execution strategy
/// replays a committed transaction, and the audit event that the budget is counted from is written
/// inside that same transaction, so the budget is right even when the boolean is not.
/// </summary>
/// <param name="repository">Ledger reads: candidates, budget counts, recent executions for the cascade guard.</param>
/// <param name="ledgerService">Ledger writes: claims, reclaims, transitions, attempt and provenance events.</param>
/// <param name="governanceResolver">Per-kind, per-scope MaxAction, owner and budget values (Etappe 4a).</param>
/// <param name="registry">Code-only map from kind to remediation; absence caps the kind at Hint.</param>
/// <param name="quietWindow">Answers whether now is a bad moment to touch this condition's target.</param>
/// <param name="identityProvider">Borrows the responsible owner's rights under Klacksy's own name (Etappe 4d).</param>
/// <param name="skillExecutor">Runs the remediation skill.</param>
/// <param name="reporter">Mandatory post-action report, never subject to the notification rate limit.</param>
/// <param name="timeProvider">Clock, injected so the budget day and the stale-claim window are testable.</param>
/// <param name="logger">Structured log per kind and per skipped row - the counterpart of "no silent caps".</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class AgentConditionActionService : IAgentConditionActionService
{
    private const string ClaimDetailFormat = "{0}skill={1} attempt={2}";
    private const string OutcomeDetailFormat = "{0}{1}";
    private const string ExecutedDetail = "executed {0}";
    private const string FailedDetail = "attempt failed: {0}";
    private const string EscalatedDetail = "ineffective after {0} attempt(s)";

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

    private const string DailyBudgetReason = "the daily action budget of {0} is used up";
    private const string WindowBudgetReason =
        "the circuit breaker tripped - {0} action(s) are allowed per {1} minute(s)";

    private const string TickCapReason =
        "the absolute cap of {0} action(s) per kind per scan is reached";

    private const string PrepareWithoutScenarioMessage =
        "Kind {Kind} is governed at Prepare, but its remediation {Skill} is execute-only and cannot be "
        + "staged as a scenario; condition {ConditionId} is reported and left to a human";

    private const string NoResultMessage = "no message";

    private static readonly AgentConditionActionTickResult EmptyResult = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private readonly IAgentConditionRepository _repository;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IProactiveGovernanceResolver _governanceResolver;
    private readonly IConditionRemediationRegistry _registry;
    private readonly IQuietWindowService _quietWindow;
    private readonly IProactiveActionIdentityProvider _identityProvider;
    private readonly ISkillExecutor _skillExecutor;
    private readonly IProactiveActionReporter _reporter;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AgentConditionActionService> _logger;

    public AgentConditionActionService(
        IAgentConditionRepository repository,
        IAgentConditionLedgerService ledgerService,
        IProactiveGovernanceResolver governanceResolver,
        IConditionRemediationRegistry registry,
        IQuietWindowService quietWindow,
        IProactiveActionIdentityProvider identityProvider,
        ISkillExecutor skillExecutor,
        IProactiveActionReporter reporter,
        TimeProvider timeProvider,
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
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AgentConditionActionTickResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var kinds = _registry.RegisteredKinds;
        if (kinds.Count == 0)
        {
            return EmptyResult;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var tally = new ActionTally();
        var recentExecutions = await _repository.GetExecutedSinceAsync(
            nowUtc.AddMinutes(-AgentConditionActionDefaults.CascadeWindowMinutes), cancellationToken);

        foreach (var triggerKind in kinds)
        {
            try
            {
                await RunKindAsync(triggerKind, nowUtc, recentExecutions, tally, cancellationToken);
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
            + "{SkippedUnbindable} unbindable, {SkippedNoOwner} without owner, {SkippedClaimLost} claim lost, "
            + "{LeftForBudget} left for budget",
            result.Considered, result.Executed, result.Failed, result.Escalated, result.SkippedCascade,
            result.SkippedQuiet, result.SkippedUnbindable, result.SkippedNoOwner, result.SkippedClaimLost,
            result.LeftForBudget);

        return result;
    }

    private async Task RunKindAsync(
        string triggerKind,
        DateTime nowUtc,
        IReadOnlyList<AgentCondition> recentExecutions,
        ActionTally tally,
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

        var budget = new ActionBudget(_repository, triggerKind, nowUtc);
        var governanceCache = new Dictionary<Guid?, ProactiveGovernanceDecision>();
        var actionsThisTick = 0;

        for (var index = 0; index < candidates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var condition = candidates[index];
            tally.Considered++;

            var governance = await ResolveGovernanceAsync(governanceCache, condition, cancellationToken);
            var maxAction = EffectiveMaxActionFor(governance, condition);
            if (maxAction < ProactiveMaxAction.Execute)
            {
                if (maxAction == ProactiveMaxAction.Prepare && !entry.IsScenarioCapable)
                {
                    _logger.LogDebug(
                        PrepareWithoutScenarioMessage, triggerKind, entry.RemediationSkillName, condition.Id);
                }

                continue;
            }

            if (await IsCascadeAsync(condition, recentExecutions, cancellationToken))
            {
                tally.SkippedCascade++;
                continue;
            }

            if (condition.AttemptCount >= AgentConditionActionDefaults.MaxAttemptsBeforeEscalation)
            {
                await EscalateAsync(condition, governance, cancellationToken);
                tally.Escalated++;
                continue;
            }

            if (await _quietWindow.IsQuietForAsync(condition, cancellationToken))
            {
                tally.SkippedQuiet++;
                continue;
            }

            var arguments = TryBindArguments(entry, condition);
            if (arguments is null)
            {
                tally.SkippedUnbindable++;
                continue;
            }

            if (governance.ResponsibleOwnerUserId is not Guid ownerUserId || ownerUserId == Guid.Empty)
            {
                _logger.LogWarning(
                    "Condition {ConditionId} of kind {Kind} is executable but its governance names no "
                    + "responsible owner, so there is no identity to act under",
                    condition.Id, triggerKind);
                tally.SkippedNoOwner++;
                continue;
            }

            if (actionsThisTick >= AgentConditionActionDefaults.MaxExecutionsPerKindPerTick)
            {
                await ReportBudgetStopAsync(
                    triggerKind,
                    ownerUserId,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        TickCapReason,
                        AgentConditionActionDefaults.MaxExecutionsPerKindPerTick),
                    candidates.Count - index,
                    cancellationToken);
                tally.LeftForBudget += candidates.Count - index;
                return;
            }

            var blockedReason = await budget.DescribeBlockAsync(governance, cancellationToken);
            if (blockedReason is not null)
            {
                await ReportBudgetStopAsync(
                    triggerKind, ownerUserId, blockedReason, candidates.Count - index, cancellationToken);
                tally.LeftForBudget += candidates.Count - index;
                return;
            }

            if (!await TryClaimAsync(condition, entry, nowUtc, cancellationToken))
            {
                tally.SkippedClaimLost++;
                continue;
            }

            budget.RecordClaim();
            actionsThisTick++;

            if (await ExecuteAsync(condition, entry, arguments, ownerUserId, cancellationToken))
            {
                tally.Executed++;
            }
            else
            {
                tally.Failed++;
            }
        }
    }

    /// <summary>
    /// Governance folded with the Etappe-4e delegation and then capped by the remediation registry.
    /// Precedence is not negotiable in two places: the global kill switch and a disabled kind pin the
    /// result at Hint BEFORE the delegation is looked at, because a human's earlier "you handle this
    /// one" grant must never survive the emergency stop; and the registry cap applies LAST, so no
    /// delegation can steer a kind that has no remediation past Hint.
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

        return _registry.TryGetEffectiveMaxAction(condition.TriggerKind, requested);
    }

    private async Task<ProactiveGovernanceDecision> ResolveGovernanceAsync(
        Dictionary<Guid?, ProactiveGovernanceDecision> cache,
        AgentCondition condition,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(condition.GroupId, out var cached))
        {
            return cached;
        }

        var decision = await _governanceResolver.ResolveAsync(
            condition.TriggerKind, condition.GroupId, cancellationToken);
        cache[condition.GroupId] = decision;

        return decision;
    }

    /// <summary>
    /// True when this row must never be auto-handled because Klacksy itself may have produced it: either
    /// it already carries a provenance link, or it was detected after a Klacksy execution on the same
    /// entity within one scan interval - in which case the link is written now, so the attribution
    /// survives this tick.
    ///
    /// Matching is on EntityId when the candidate has one and falls back to GroupId only when it does
    /// not. Matching every row of a group would suppress genuinely unrelated findings for an hour AND
    /// stamp a false provenance claim into the ledger, which is worse than a missed automation: the
    /// cascade guard exists to catch "my fix broke the thing I touched", not "something happened
    /// nearby".
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
    /// The remediation's arguments, or null when this condition cannot produce them. Null is NOT a
    /// failure to be retried: a payload that lacks what the binder needs will lack it forever, because a
    /// re-detection only moves LastSeenAtUtc and never rewrites PayloadJson. Every row already open when
    /// a binder gains a new required field lands here, which is exactly why the check runs before the
    /// claim - otherwise deploying a binder change would burn three attempts and an escalation on the
    /// entire existing backlog.
    /// </summary>
    private IReadOnlyDictionary<string, object?>? TryBindArguments(
        ConditionRemediationEntry entry, AgentCondition condition)
    {
        Dictionary<string, object?>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(condition.PayloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex, "Condition {ConditionId} carries a payload that is not valid JSON and cannot be remediated",
                condition.Id);
            return null;
        }

        if (payload is null)
        {
            return null;
        }

        var arguments = entry.ParameterBinder.Bind(payload);
        var missing = entry.RequiredArguments
            .Where(name => !arguments.TryGetValue(name, out var value) || value is null)
            .ToList();

        if (missing.Count == 0)
        {
            return arguments;
        }

        _logger.LogInformation(
            "Condition {ConditionId} cannot be remediated by {Skill}: the payload yields no {Missing}. "
            + "It stays reported and visible to planners, and costs neither an attempt nor action budget",
            condition.Id, entry.RemediationSkillName, string.Join(", ", missing));

        return null;
    }

    /// <summary>
    /// Takes the row for this run. A Reported row is moved to Prepared; a Prepared row is a claim
    /// somebody else made, and is taken over only when it has gone stale - both raise AttemptCount and
    /// stamp LastAttemptAtUtc inside the same conditional UPDATE. Nothing here reads the row first to
    /// decide: the read-then-decide version is precisely the race two instances would lose.
    /// </summary>
    private async Task<bool> TryClaimAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
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
                userId: null,
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
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var identity = await _identityProvider.ResolveForSkillAsync(
            ownerUserId, condition.Id, entry.RemediationSkillName, cancellationToken);

        if (!identity.Success || identity.Context is null)
        {
            await RecordFailureAsync(condition, entry, ownerUserId, identity.Reason ?? string.Empty, cancellationToken);
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
            await RecordFailureAsync(condition, entry, ownerUserId, ex.Message, cancellationToken);
            return false;
        }

        var message = string.IsNullOrWhiteSpace(result.Message) ? NoResultMessage : result.Message!;
        if (!result.Success)
        {
            await RecordFailureAsync(condition, entry, ownerUserId, message, cancellationToken);
            return false;
        }

        await _ledgerService.TryTransitionAsync(
            condition.Id,
            AgentConditionStatus.Prepared,
            AgentConditionStatus.Executed,
            userId: null,
            detail: Outcome(string.Format(CultureInfo.InvariantCulture, ExecutedDetail, entry.RemediationSkillName)),
            fields: new AgentConditionTransitionFields(HandlingKind: AgentConditionHandlingKind.Executed),
            cancellationToken);

        await _reporter.ReportAsync(
            ownerUserId,
            string.Format(
                CultureInfo.InvariantCulture,
                ExecutedReportFormat,
                condition.TriggerKind,
                condition.Id,
                entry.RemediationSkillName,
                message),
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
        Guid ownerUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        var attempt = condition.AttemptCount + 1;

        await _ledgerService.RecordEventAsync(
            condition.Id,
            AgentConditionEventTypes.AttemptFailed,
            Outcome(string.Format(CultureInfo.InvariantCulture, FailedDetail, reason)),
            cancellationToken);

        await _reporter.ReportAsync(
            ownerUserId,
            string.Format(
                CultureInfo.InvariantCulture,
                FailedReportFormat,
                condition.TriggerKind,
                condition.Id,
                entry.RemediationSkillName,
                reason,
                attempt,
                AgentConditionActionDefaults.MaxAttemptsBeforeEscalation),
            cancellationToken);
    }

    private async Task EscalateAsync(
        AgentCondition condition, ProactiveGovernanceDecision governance, CancellationToken cancellationToken)
    {
        var escalated = await _ledgerService.TryTransitionAsync(
            condition.Id,
            condition.Status,
            AgentConditionStatus.Escalated,
            userId: null,
            detail: Outcome(string.Format(CultureInfo.InvariantCulture, EscalatedDetail, condition.AttemptCount)),
            cancellationToken: cancellationToken);

        if (!escalated || governance.ResponsibleOwnerUserId is not Guid ownerUserId || ownerUserId == Guid.Empty)
        {
            return;
        }

        await _reporter.ReportAsync(
            ownerUserId,
            string.Format(
                CultureInfo.InvariantCulture,
                EscalatedReportFormat,
                condition.TriggerKind,
                condition.Id,
                condition.AttemptCount),
            cancellationToken);
    }

    /// <summary>
    /// A budget stop is reported, never only counted. Silently leaving findings unhandled is exactly the
    /// failure mode this stage was told not to have: the planner would see an automation that works on
    /// some days and does nothing on others, with no way to tell which.
    /// </summary>
    private async Task ReportBudgetStopAsync(
        string triggerKind,
        Guid ownerUserId,
        string reason,
        int remaining,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Proactive actions on {Kind} stopped: {Reason}. {Remaining} finding(s) stay open this tick",
            triggerKind, reason, remaining);

        await _reporter.ReportAsync(
            ownerUserId,
            string.Format(CultureInfo.InvariantCulture, BudgetReportFormat, triggerKind, reason, remaining),
            cancellationToken);
    }

    private static string Outcome(string detail) =>
        string.Format(
            CultureInfo.InvariantCulture,
            OutcomeDetailFormat,
            AgentConditionActionDefaults.ActionOutcomeDetailPrefix,
            detail);

    private sealed class ActionTally
    {
        public int Considered { get; set; }

        public int Executed { get; set; }

        public int Failed { get; set; }

        public int Escalated { get; set; }

        public int SkippedCascade { get; set; }

        public int SkippedQuiet { get; set; }

        public int SkippedUnbindable { get; set; }

        public int SkippedNoOwner { get; set; }

        public int SkippedClaimLost { get; set; }

        public int LeftForBudget { get; set; }

        public AgentConditionActionTickResult ToResult() => new(
            Considered, Executed, Failed, Escalated, SkippedCascade, SkippedQuiet,
            SkippedUnbindable, SkippedNoOwner, SkippedClaimLost, LeftForBudget);
    }

    /// <summary>
    /// The daily budget and the circuit breaker for one kind in one tick. Both are counted in the
    /// database from the claims' audit events, so several API instances share one budget; the claims
    /// this tick has made itself are added on top, because the queries ran before them. Window counts
    /// are cached per window length: governance may configure a different window per scope, and the
    /// same length must not be re-queried for every condition.
    /// </summary>
    private sealed class ActionBudget
    {
        private readonly IAgentConditionRepository _repository;
        private readonly string _triggerKind;
        private readonly DateTime _nowUtc;
        private readonly Dictionary<int, int> _windowCounts = new();
        private int? _todayCount;
        private int _claimsThisTick;

        public ActionBudget(IAgentConditionRepository repository, string triggerKind, DateTime nowUtc)
        {
            _repository = repository;
            _triggerKind = triggerKind;
            _nowUtc = nowUtc;
        }

        public void RecordClaim() => _claimsThisTick++;

        /// <summary>Why this kind may not act right now, or null when it may.</summary>
        public async Task<string?> DescribeBlockAsync(
            ProactiveGovernanceDecision governance, CancellationToken cancellationToken)
        {
            _todayCount ??= await _repository.CountActionClaimsAsync(
                _triggerKind, _nowUtc.Date, cancellationToken);

            if (_todayCount.Value + _claimsThisTick >= governance.DailyActionBudget)
            {
                return string.Format(
                    CultureInfo.InvariantCulture, DailyBudgetReason, governance.DailyActionBudget);
            }

            if (!_windowCounts.TryGetValue(governance.WindowMinutes, out var windowCount))
            {
                windowCount = await _repository.CountActionClaimsAsync(
                    _triggerKind, _nowUtc.AddMinutes(-governance.WindowMinutes), cancellationToken);
                _windowCounts[governance.WindowMinutes] = windowCount;
            }

            if (windowCount + _claimsThisTick >= governance.WindowActionLimit)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    WindowBudgetReason,
                    governance.WindowActionLimit,
                    governance.WindowMinutes);
            }

            return null;
        }
    }
}
