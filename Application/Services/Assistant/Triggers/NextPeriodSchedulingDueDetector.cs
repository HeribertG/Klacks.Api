// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects groups whose NEXT pay-period starts within NextPeriodScheduling.LeadTimeDays and has no
/// AnalyseScenario covering it yet. Period boundaries follow the group's PaymentInterval exactly like
/// PeriodCloseDueDetector, shifted one period into the future; Individual is skipped (custom, no
/// derivable cycle), as are groups without any clients or shifts in themselves or a descendant group,
/// and — since 2026-09-07 — groups whose next period contains no shift that could be planned at all.
/// The gate deliberately asks about SHIFTS and not about work: this trigger exists precisely because
/// nothing has been planned yet, so gating it on existing assignments would switch it off forever.
/// A period with no shift in it is a different case — there is nothing to staff, and both the hint
/// and the autofill chain below it would have no input.
/// While the EMAIL_ANALYSIS_ENABLED setting is active, an unprocessed inbox backlog defers the whole
/// scan one tick, because availability/day-off mail may not be incorporated yet. Whether the detector
/// may start the AutoWizard chain itself is NOT decided here: it reads CanStartAutofill off the shared
/// INextPeriodAutonomyResolver decision, which already folds all four brakes — the global kill switch,
/// the global autonomy level, and the Enabled/MaxAction pair of this kind's governance row — together
/// with the minimum autonomy level over all admin users. The decision is resolved once per tick and
/// reused for every group, because all of its inputs are installation-wide.
/// When CanStartAutofill holds, the chain runs fire-and-forget inside the runner (it produces a draft
/// scenario a human must accept) and an informative NextPeriodAutofillStartedTriggerEvent is emitted;
/// otherwise, or when the automatic start is not possible, a NextPeriodSchedulingDueTriggerEvent hint
/// is emitted instead — the hint branch is never gated, findings keep being reported. When CanCommit
/// holds as well, the produced scenario is additionally handed to INextPeriodAutoCommitService, which
/// accepts it into the real schedule only when it introduces zero new compliance issues.
/// A period already covered by a scenario is skipped, with one exception: when that scenario is a
/// still-unaccepted draft of an automatic run whose watcher is gone (an API restart), the tick reports
/// it once as an interrupted auto-commit. It is never silently re-committed — the accept a dead watcher
/// would have made is exactly the decision that now needs a human.
/// </summary>
/// <param name="groupRepository">Lists all groups (filters out deleted via query filter).</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly period boundaries.</param>
/// <param name="scenarioRepository">Checks whether a scenario already covers the next period.</param>
/// <param name="activityProbe">Answers whether the next period holds any plannable shift at all.</param>
/// <param name="autoWizardJobRunner">Starts the Wizard 1+2+3 chain when autonomy permits.</param>
/// <param name="clientRepository">Resolves the group's active clients as wizard agents.</param>
/// <param name="shiftScheduleRepository">Resolves the group's visible shifts for the period.</param>
/// <param name="autoCommitService">Watches a started chain and auto-accepts when the commit gate holds.</param>
/// <param name="autonomyResolver">All four autonomy brakes folded into one decision, shared with the watcher.</param>
/// <param name="conditionRepository">Open ledger rows of this kind, read to recognise an interrupted auto-commit.</param>
/// <param name="settingsReader">Reads the EMAIL_ANALYSIS_ENABLED setting.</param>
/// <param name="receivedEmailRepository">Probes for unprocessed inbox mail.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day, not the server's UTC day.</param>
/// <param name="timeProvider">Ages the ledger row of an automatic start against the interrupted grace window.</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;
using AppSettings = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class NextPeriodSchedulingDueDetector : IAgentTriggerDetector
{
    private const int WeeklyPeriodDays = 7;
    private const int BiweeklyCycleDays = 14;
    private const int UnprocessedEmailProbeCount = 1;
    private const int NoNewComplianceIssues = 0;

    private readonly IGroupRepository _groupRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly IAutoWizardJobRunner _autoWizardJobRunner;
    private readonly IClientRepository _clientRepository;
    private readonly IShiftScheduleRepository _shiftScheduleRepository;
    private readonly INextPeriodAutoCommitService _autoCommitService;
    private readonly INextPeriodAutonomyResolver _autonomyResolver;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly ISettingsReader _settingsReader;
    private readonly IReceivedEmailRepository _receivedEmailRepository;
    private readonly ILogger<NextPeriodSchedulingDueDetector> _logger;
    private readonly ICompanyClock _companyClock;
    private readonly TimeProvider _timeProvider;

    public NextPeriodSchedulingDueDetector(
        IGroupRepository groupRepository,
        IWeekConfiguration weekConfiguration,
        IAnalyseScenarioRepository scenarioRepository,
        IScheduleActivityProbe activityProbe,
        IAutoWizardJobRunner autoWizardJobRunner,
        IClientRepository clientRepository,
        IShiftScheduleRepository shiftScheduleRepository,
        INextPeriodAutoCommitService autoCommitService,
        INextPeriodAutonomyResolver autonomyResolver,
        IAgentConditionRepository conditionRepository,
        ISettingsReader settingsReader,
        IReceivedEmailRepository receivedEmailRepository,
        ILogger<NextPeriodSchedulingDueDetector> logger,
        ICompanyClock companyClock,
        TimeProvider timeProvider)
    {
        _groupRepository = groupRepository;
        _weekConfiguration = weekConfiguration;
        _scenarioRepository = scenarioRepository;
        _activityProbe = activityProbe;
        _autoWizardJobRunner = autoWizardJobRunner;
        _clientRepository = clientRepository;
        _shiftScheduleRepository = shiftScheduleRepository;
        _autoCommitService = autoCommitService;
        _autonomyResolver = autonomyResolver;
        _conditionRepository = conditionRepository;
        _settingsReader = settingsReader;
        _receivedEmailRepository = receivedEmailRepository;
        _logger = logger;
        _companyClock = companyClock;
        _timeProvider = timeProvider;
    }

    public string Kind => AgentTriggerKinds.NextPeriodSchedulingDue;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var groups = await _groupRepository.List();
        if (groups.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        if (await HasUnprocessedEmailBacklogAsync())
        {
            // Availability and day-off mail may not be incorporated yet; deferring one tick is
            // cheaper than planning on stale input and is only active while email analysis is on.
            _logger.LogInformation(
                "NextPeriodSchedulingDue scan deferred: unprocessed email backlog while email analysis is enabled");

            return Array.Empty<IAgentTriggerEvent>();
        }

        var weekStart = await _weekConfiguration.GetWeekStartAsync(today, cancellationToken);
        var nextWeekStart = weekStart.AddDays(WeeklyPeriodDays);
        var staffing = GroupStaffingLookup.Build(
            groups,
            await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));

        NextPeriodAutonomyDecision? autonomy = null;
        var events = new List<IAgentTriggerEvent>();
        var autofillStarts = 0;
        var skippedWithoutShifts = 0;
        var interruptedAutoCommits = 0;
        List<AgentCondition>? openConditions = null;

        foreach (var group in groups)
        {
            if (group.PaymentInterval == PaymentInterval.Individual)
            {
                _logger.LogDebug(
                    "NextPeriodSchedulingDue: group {GroupName} uses PaymentInterval Individual, which has no derivable cycle — skipped",
                    group.Name);

                continue;
            }

            if (!staffing.IsStaffed(group.Id)) continue;

            var periodStart = ComputeNextPeriodStart(group, today, nextWeekStart);
            var daysUntilStart = periodStart.DayNumber - today.DayNumber;
            if (daysUntilStart > NextPeriodScheduling.LeadTimeDays) continue;

            var periodEnd = ComputeNextPeriodEnd(group, periodStart);
            var covering = await FindCoveringScenarioAsync(group.Id, periodStart, periodEnd, cancellationToken);
            if (covering != null)
            {
                if (covering.Status == AnalyseScenarioStatus.Active)
                {
                    openConditions ??= await _conditionRepository.GetOpenByKindAsync(Kind, cancellationToken);
                    var interrupted = FindInterruptedAutoCommit(group, periodStart, periodEnd, covering, openConditions);
                    if (interrupted != null)
                    {
                        events.Add(interrupted);
                        interruptedAutoCommits++;
                    }
                }

                continue;
            }

            if (!await _activityProbe.HasPlannableShiftsInRangeAsync(group, periodStart, periodEnd, cancellationToken))
            {
                skippedWithoutShifts++;
                continue;
            }

            autonomy ??= await _autonomyResolver.ResolveAsync(cancellationToken);
            if (autonomy.CanStartAutofill)
            {
                var (startedEvent, fallBackToHint) =
                    await TryStartAutofillAsync(group, periodStart, periodEnd, autonomy.CanCommit, cancellationToken);
                if (startedEvent != null)
                {
                    events.Add(startedEvent);
                    autofillStarts++;
                    continue;
                }

                if (!fallBackToHint) continue;
            }

            events.Add(new NextPeriodSchedulingDueTriggerEvent(
                group.Id,
                group.Name,
                periodStart,
                periodEnd,
                daysUntilStart));
        }

        _logger.LogInformation(
            "NextPeriodSchedulingDue scan: {Total} group(s) scanned, {Events} event(s) emitted, {Autofills} autofill run(s) started, {SkippedWithoutShifts} skipped because the next period holds no plannable shift, {Interrupted} interrupted auto-commit(s) reported",
            groups.Count, events.Count, autofillStarts, skippedWithoutShifts, interruptedAutoCommits);

        return events;
    }

    private async Task<bool> HasUnprocessedEmailBacklogAsync()
    {
        var setting = await _settingsReader.GetSetting(AppSettings.EMAIL_ANALYSIS_ENABLED);
        var emailAnalysisEnabled = setting?.Value != null
            && bool.TryParse(setting.Value, out var enabled)
            && enabled;
        if (!emailAnalysisEnabled)
        {
            return false;
        }

        var backlog = await _receivedEmailRepository.GetUnprocessedAsync(UnprocessedEmailProbeCount);
        return backlog.Count > 0;
    }

    /// <summary>
    /// An automatic run whose watcher never reported an outcome, recognised WITHOUT re-committing
    /// anything. Four conditions have to hold together, and each one rules out a different look-alike:
    /// the run was started automatically WITH an auto-commit intent (its ledger row says so - a manual
    /// draft or an Autonomous-level run was never going to be committed); the wizard job is no longer in
    /// the registry (nothing is working on it here); no commit outcome was ever reported for this group
    /// and period (a watcher that finished and blocked leaves exactly the same draft behind, and must not
    /// be reported a second time under a different name); and the run is older than the grace window,
    /// because the job registry is per API instance - inside that window a watcher on another instance
    /// may legitimately still be working on the very same chain.
    /// The residual false positive this cannot see: once every commit-outcome row of the period has been
    /// dismissed by a planner it is no longer open, and one interrupted event can be raised on top of it.
    /// </summary>
    private NextPeriodAutoCommitBlockedTriggerEvent? FindInterruptedAutoCommit(
        Group group,
        DateOnly periodStart,
        DateOnly periodEnd,
        AnalyseScenario covering,
        IReadOnlyList<AgentCondition> openConditions)
    {
        var autofillFingerprint = AgentConditionLedgerPolicy.FingerprintFor(
            Kind, NextPeriodAutofillStartedTriggerEvent.DedupKeyFor(group.Id, periodStart));
        var autofillRow = openConditions.FirstOrDefault(row =>
            string.Equals(row.Fingerprint, autofillFingerprint, StringComparison.Ordinal));
        if (autofillRow == null || !TryReadAutoCommitIntent(autofillRow.PayloadJson, out var jobId))
        {
            return null;
        }

        if (_autoWizardJobRunner.IsRunning(jobId))
        {
            return null;
        }

        var outcomePrefix = AgentConditionLedgerPolicy.FingerprintFor(
            Kind, NextPeriodAutoCommitBlockedTriggerEvent.CommitOutcomeDedupPrefix(group.Id, periodStart));
        if (openConditions.Any(row => row.Fingerprint.StartsWith(outcomePrefix, StringComparison.Ordinal)))
        {
            return null;
        }

        var graceEnd = autofillRow.DetectedAtUtc.AddMinutes(NextPeriodScheduling.AutoCommitInterruptedGraceMinutes);
        if (_timeProvider.GetUtcNow().UtcDateTime < graceEnd)
        {
            return null;
        }

        _logger.LogWarning(
            "NextPeriodSchedulingDue: automatic autofill job {JobId} for group {GroupName} left scenario {ScenarioId} unaccepted and is no longer being watched; reporting it for manual review",
            jobId, group.Name, covering.Id);

        return new NextPeriodAutoCommitBlockedTriggerEvent(
            group.Id,
            group.Name,
            periodStart,
            periodEnd,
            covering.Id,
            NoNewComplianceIssues,
            NextPeriodAutoCommitBlockReason.Interrupted);
    }

    /// <summary>
    /// Reads the auto-commit intent and the job id out of an autofill ledger payload. False for a payload
    /// that is unreadable, carries no intent or names no job - all three mean the same thing here, that
    /// this row cannot establish an interrupted auto-commit, and a malformed row must never be guessed at.
    /// </summary>
    private static bool TryReadAutoCommitIntent(string payloadJson, out Guid jobId)
    {
        jobId = Guid.Empty;
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!root.TryGetProperty(NextPeriodAutofillStartedTriggerEvent.AutoCommitIntendedPayloadKey, out var intended)
                || intended.ValueKind != JsonValueKind.True)
            {
                return false;
            }

            return root.TryGetProperty(NextPeriodAutofillStartedTriggerEvent.JobIdPayloadKey, out var job)
                && job.TryGetGuid(out jobId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// The scenario that already covers this period, preferring an Accepted one over a still-open draft.
    /// The preference is not cosmetic: an Accepted scenario means the period IS committed, and returning
    /// a draft that happens to sit next to it would let the interrupted check report a committed period
    /// as unfinished.
    /// </summary>
    private async Task<AnalyseScenario?> FindCoveringScenarioAsync(
        Guid groupId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
    {
        var scenarios = await _scenarioRepository.GetByGroupAsync(groupId, cancellationToken);
        var covering = scenarios
            .Where(scenario =>
                (scenario.Status == AnalyseScenarioStatus.Active || scenario.Status == AnalyseScenarioStatus.Accepted)
                && scenario.FromDate <= periodStart
                && scenario.UntilDate >= periodEnd)
            .ToList();

        return covering.FirstOrDefault(scenario => scenario.Status == AnalyseScenarioStatus.Accepted)
            ?? covering.FirstOrDefault();
    }

    private async Task<(NextPeriodAutofillStartedTriggerEvent? StartedEvent, bool FallBackToHint)> TryStartAutofillAsync(
        Group group, DateOnly periodStart, DateOnly periodEnd, bool autoCommit, CancellationToken cancellationToken)
    {
        var clients = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(
            new List<Guid> { group.Id }, cancellationToken);
        var agentIds = clients.Select(client => client.Id).Distinct().ToList();
        if (agentIds.Count == 0)
        {
            _logger.LogInformation(
                "NextPeriodSchedulingDue: group {GroupName} has no active clients, automatic autofill not possible — falling back to hint",
                group.Name);

            return (null, true);
        }

        var filter = new ShiftScheduleFilter
        {
            StartDate = periodStart,
            EndDate = periodEnd,
            SelectedGroup = group.Id,
            AnalyseToken = null,
            StartRow = 0,
            RowCount = int.MaxValue
        };
        var (shifts, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(filter, cancellationToken);
        var shiftIds = shifts.Select(shift => shift.ShiftId).Distinct().ToList();
        if (shiftIds.Count == 0)
        {
            _logger.LogInformation(
                "NextPeriodSchedulingDue: group {GroupName} has no visible shifts in {From}..{Until}, automatic autofill not possible — falling back to hint",
                group.Name, periodStart, periodEnd);

            return (null, true);
        }

        var request = new StartAutoWizardRequest(
            PeriodFrom: periodStart,
            PeriodUntil: periodEnd,
            AgentIds: agentIds,
            ShiftIds: shiftIds,
            GroupId: group.Id,
            AnalyseToken: null,
            Language: null);

        try
        {
            var jobId = await _autoWizardJobRunner.StartAsync(request, CancellationToken.None);
            _logger.LogInformation(
                "NextPeriodSchedulingDue: automatic autofill job {JobId} started for group {GroupName}, period {From}..{Until} ({Agents} agents, {Shifts} shifts, autoCommit {AutoCommit})",
                jobId, group.Name, periodStart, periodEnd, agentIds.Count, shiftIds.Count, autoCommit);

            if (autoCommit)
            {
                _autoCommitService.QueueAutoCommit(jobId, group.Id, group.Name, periodStart, periodEnd);
            }

            return (new NextPeriodAutofillStartedTriggerEvent(
                group.Id, group.Name, periodStart, periodEnd, jobId, autoCommit), false);
        }
        catch (AutofillRunConflictException ex)
        {
            // A run for this period is already underway, so planning is in progress — neither a
            // second start nor a hint would add anything.
            _logger.LogInformation(
                "NextPeriodSchedulingDue: a {Family} job is already running for group {GroupName} (jobId {JobId}), nothing to do",
                ex.Family, group.Name, ex.RunningJobId);

            return (null, false);
        }
        catch (AutofillLimitExceededException ex)
        {
            _logger.LogWarning(
                "NextPeriodSchedulingDue: automatic autofill for group {GroupName} exceeds the configured limits ({Reason}) — falling back to hint",
                group.Name, ex.Message);

            return (null, true);
        }
    }

    private static DateOnly ComputeNextPeriodStart(Group group, DateOnly today, DateOnly nextWeekStart)
    {
        return group.PaymentInterval switch
        {
            PaymentInterval.Weekly => nextWeekStart,
            PaymentInterval.Biweekly => EndOfBiweekly(today, group.ValidFrom).AddDays(1),
            PaymentInterval.Monthly => FirstOfNextMonth(today),
            PaymentInterval.MonthlyTargetHours => FirstOfNextMonth(today),
            _ => throw new ArgumentOutOfRangeException(nameof(group),
                $"Unsupported PaymentInterval '{group.PaymentInterval}' — caller must filter Individual.")
        };
    }

    private static DateOnly ComputeNextPeriodEnd(Group group, DateOnly periodStart)
    {
        return group.PaymentInterval switch
        {
            PaymentInterval.Weekly => periodStart.AddDays(WeeklyPeriodDays - 1),
            PaymentInterval.Biweekly => periodStart.AddDays(BiweeklyCycleDays - 1),
            PaymentInterval.Monthly => EndOfMonth(periodStart),
            PaymentInterval.MonthlyTargetHours => EndOfMonth(periodStart),
            _ => throw new ArgumentOutOfRangeException(nameof(group),
                $"Unsupported PaymentInterval '{group.PaymentInterval}' — caller must filter Individual.")
        };
    }

    private static DateOnly FirstOfNextMonth(DateOnly today)
    {
        return new DateOnly(today.Year, today.Month, 1).AddMonths(1);
    }

    private static DateOnly EndOfMonth(DateOnly date)
    {
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        return new DateOnly(date.Year, date.Month, daysInMonth);
    }

    private static DateOnly EndOfBiweekly(DateOnly today, DateTime groupAnchor)
    {
        var anchor = DateOnly.FromDateTime(groupAnchor);
        var daysSinceAnchor = today.DayNumber - anchor.DayNumber;
        var positionInCycle = ((daysSinceAnchor % BiweeklyCycleDays) + BiweeklyCycleDays) % BiweeklyCycleDays;
        return today.AddDays(BiweeklyCycleDays - 1 - positionInCycle);
    }
}
