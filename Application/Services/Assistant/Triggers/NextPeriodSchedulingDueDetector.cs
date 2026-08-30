// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects groups whose NEXT pay-period starts within NextPeriodScheduling.LeadTimeDays and has no
/// AnalyseScenario covering it yet. Period boundaries follow the group's PaymentInterval exactly like
/// PeriodCloseDueDetector, shifted one period into the future; Individual is skipped (custom, no
/// derivable cycle), as are groups without any clients or shifts in themselves or a descendant group.
/// While the EMAIL_ANALYSIS_ENABLED setting is active, an unprocessed inbox backlog defers the whole
/// scan one tick, because availability/day-off mail may not be incorporated yet. At an effective
/// autonomy level of Autonomous or higher — the minimum over all admin users, capped by the global
/// proactive autonomy level, the same aggregation EmailActionOrchestrator applies — the detector starts the AutoWizard chain itself (fire-and-forget
/// inside the runner; it produces a draft scenario a human must accept) and emits an informative
/// NextPeriodAutofillStartedTriggerEvent; below that, or when the automatic start is not possible, it
/// emits a NextPeriodSchedulingDueTriggerEvent hint. At FullyAutonomous the produced scenario is
/// additionally handed to INextPeriodAutoCommitService, which accepts it into the real schedule only
/// when it introduces zero new compliance issues. The global proactive kill switch pins the whole
/// tick to the hint-only branch, exactly like every governed trigger kind — checked once per tick,
/// not per group, since it is a single settings read shared by the whole scan.
/// </summary>
/// <param name="groupRepository">Lists all groups (filters out deleted via query filter).</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly period boundaries.</param>
/// <param name="scenarioRepository">Checks whether a scenario already covers the next period.</param>
/// <param name="autoWizardJobRunner">Starts the Wizard 1+2+3 chain when autonomy permits.</param>
/// <param name="clientRepository">Resolves the group's active clients as wizard agents.</param>
/// <param name="shiftScheduleRepository">Resolves the group's visible shifts for the period.</param>
/// <param name="autoCommitService">Watches a started chain and auto-accepts at FullyAutonomous.</param>
/// <param name="audienceResolver">Resolves the admin users whose autonomy levels are aggregated.</param>
/// <param name="autonomyPreferences">Per-admin autonomy level rows (default when absent).</param>
/// <param name="governanceResolver">Source of the global proactive kill switch.</param>
/// <param name="settingsReader">Reads the EMAIL_ANALYSIS_ENABLED setting.</param>
/// <param name="receivedEmailRepository">Probes for unprocessed inbox mail.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="timeProvider">Clock used to derive today.</param>

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
using Klacks.Api.Domain.Models.Associations;
using AppSettings = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class NextPeriodSchedulingDueDetector : IAgentTriggerDetector
{
    private const int WeeklyPeriodDays = 7;
    private const int BiweeklyCycleDays = 14;
    private const int UnprocessedEmailProbeCount = 1;
    private const AutonomyLevel AutoRunMinimumLevel = AutonomyLevel.Autonomous;

    private readonly IGroupRepository _groupRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAutoWizardJobRunner _autoWizardJobRunner;
    private readonly IClientRepository _clientRepository;
    private readonly IShiftScheduleRepository _shiftScheduleRepository;
    private readonly INextPeriodAutoCommitService _autoCommitService;
    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly IAgentAutonomyPreferenceRepository _autonomyPreferences;
    private readonly IProactiveGovernanceResolver _governanceResolver;
    private readonly ISettingsReader _settingsReader;
    private readonly IReceivedEmailRepository _receivedEmailRepository;
    private readonly ILogger<NextPeriodSchedulingDueDetector> _logger;
    private readonly TimeProvider _timeProvider;

    public NextPeriodSchedulingDueDetector(
        IGroupRepository groupRepository,
        IWeekConfiguration weekConfiguration,
        IAnalyseScenarioRepository scenarioRepository,
        IAutoWizardJobRunner autoWizardJobRunner,
        IClientRepository clientRepository,
        IShiftScheduleRepository shiftScheduleRepository,
        INextPeriodAutoCommitService autoCommitService,
        IPlanningAudienceResolver audienceResolver,
        IAgentAutonomyPreferenceRepository autonomyPreferences,
        IProactiveGovernanceResolver governanceResolver,
        ISettingsReader settingsReader,
        IReceivedEmailRepository receivedEmailRepository,
        ILogger<NextPeriodSchedulingDueDetector> logger,
        TimeProvider timeProvider)
    {
        _groupRepository = groupRepository;
        _weekConfiguration = weekConfiguration;
        _scenarioRepository = scenarioRepository;
        _autoWizardJobRunner = autoWizardJobRunner;
        _clientRepository = clientRepository;
        _shiftScheduleRepository = shiftScheduleRepository;
        _autoCommitService = autoCommitService;
        _audienceResolver = audienceResolver;
        _autonomyPreferences = autonomyPreferences;
        _governanceResolver = governanceResolver;
        _settingsReader = settingsReader;
        _receivedEmailRepository = receivedEmailRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public string Kind => AgentTriggerKinds.NextPeriodSchedulingDue;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
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
        var killSwitchActive = await _governanceResolver.IsKillSwitchActiveAsync(cancellationToken);

        AutonomyLevel? effectiveLevel = null;
        var events = new List<IAgentTriggerEvent>();
        var autofillStarts = 0;

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
            if (await ScenarioCoversPeriodAsync(group.Id, periodStart, periodEnd, cancellationToken)) continue;

            effectiveLevel ??= await ResolveEffectiveAutonomyLevelAsync(cancellationToken);
            if (!killSwitchActive && effectiveLevel >= AutoRunMinimumLevel)
            {
                var autoCommit = effectiveLevel == AutonomyLevel.FullyAutonomous;
                var (startedEvent, fallBackToHint) =
                    await TryStartAutofillAsync(group, periodStart, periodEnd, autoCommit, cancellationToken);
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
            "NextPeriodSchedulingDue scan: {Total} group(s) scanned, {Events} event(s) emitted, {Autofills} autofill run(s) started",
            groups.Count, events.Count, autofillStarts);

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
    /// The minimum autonomy level over all admin users, additionally capped by the global proactive
    /// autonomy level — one cautious admin throttles the automatic start for everybody, the same
    /// aggregation EmailActionOrchestrator applies. No admins means no one has consented to automation,
    /// so the level degrades to Propose.
    /// </summary>
    private async Task<AutonomyLevel> ResolveEffectiveAutonomyLevelAsync(CancellationToken cancellationToken)
    {
        var adminIds = await _audienceResolver.GetAdminUserIdsAsync(cancellationToken);
        if (adminIds.Count == 0)
        {
            return AutonomyLevel.Propose;
        }

        var minimum = AutonomyLevel.FullyAutonomous;
        foreach (var adminId in adminIds)
        {
            var row = await _autonomyPreferences.GetAsync(adminId, cancellationToken);
            var level = row?.Level ?? AutonomyDefaults.DefaultLevel;
            if (level < minimum)
            {
                minimum = level;
            }
        }

        var globalLevel = await _governanceResolver.GetGlobalAutonomyLevelAsync(cancellationToken);
        return globalLevel < minimum ? globalLevel : minimum;
    }

    private async Task<bool> ScenarioCoversPeriodAsync(
        Guid groupId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken)
    {
        var scenarios = await _scenarioRepository.GetByGroupAsync(groupId, cancellationToken);
        return scenarios.Any(scenario =>
            (scenario.Status == AnalyseScenarioStatus.Active || scenario.Status == AnalyseScenarioStatus.Accepted)
            && scenario.FromDate <= periodStart
            && scenario.UntilDate >= periodEnd);
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
