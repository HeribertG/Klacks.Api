// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Aggregates the condition ledger into one daily inbox message per planner. Scheduling state (has today
/// already run, has the configured local time of day passed) lives in the plain Settings key-value table
/// - never the encrypted settings store, which is not dump-portable - as a single watermark row advanced
/// with a compare-and-swap, so a restart never re-sends today's digest and two API instances racing the
/// same tick can never both send it. Per planner, "visible" reuses AgentConditionScopeResolver /
/// AgentConditionRepository.GetOpenForScopeAsync verbatim (Etappe 3f/3g), so the digest agrees with
/// list_open_findings and the context block about what a given planner may see, by construction.
/// </summary>
/// <param name="conditionRepository">Scoped, planner-relevant ledger reads (Etappe 3f/3g machinery).</param>
/// <param name="scopeResolver">Resolves one user's GroupVisibility scope for the ledger.</param>
/// <param name="planningAudienceResolver">Enumerates every planner (Admin + Authorised) to iterate over.</param>
/// <param name="triggerService">Persists and delivers one event per planner through the ordinary proactive pipeline.</param>
/// <param name="settingsRepository">Reads/advances the persisted "last digest date" watermark and the installation time zone settings.</param>
/// <param name="unitOfWork">Commits the watermark row the first time it is seeded on a fresh installation.</param>
/// <param name="options">Carries the configurable local time of day the digest fires.</param>
/// <param name="timeProvider">Clock, injected for deterministic testing.</param>
/// <param name="logger">Structured log per run.</param>

using System.Globalization;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public class AgentConditionDigestService : IAgentConditionDigestService
{
    private const string DateKeyFormat = "yyyy-MM-dd";
    private const string NeverRanMarker = "";

    private static readonly TimeSpan FallbackTimeOfDay =
        TimeSpan.Parse(AgentConditionDigestDefaults.DefaultTimeOfDayLocal, CultureInfo.InvariantCulture);

    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IAgentConditionScopeResolver _scopeResolver;
    private readonly IPlanningAudienceResolver _planningAudienceResolver;
    private readonly IAgentTriggerService _triggerService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BackgroundServiceOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AgentConditionDigestService> _logger;

    public AgentConditionDigestService(
        IAgentConditionRepository conditionRepository,
        IAgentConditionScopeResolver scopeResolver,
        IPlanningAudienceResolver planningAudienceResolver,
        IAgentTriggerService triggerService,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        IOptions<BackgroundServiceOptions> options,
        TimeProvider timeProvider,
        ILogger<AgentConditionDigestService> logger)
    {
        _conditionRepository = conditionRepository;
        _scopeResolver = scopeResolver;
        _planningAudienceResolver = planningAudienceResolver;
        _triggerService = triggerService;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AgentConditionDigestRunResult> RunIfDueAsync(CancellationToken cancellationToken = default)
    {
        var timeZone = await ResolveTimeZoneAsync();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var todayKey = nowLocal.ToString(DateKeyFormat, CultureInfo.InvariantCulture);

        var lastRunValue = await ReadOrSeedLastRunMarkerAsync(cancellationToken);
        if (lastRunValue == todayKey)
        {
            return new AgentConditionDigestRunResult(AgentConditionDigestOutcome.AlreadyRanToday, 0);
        }

        if (!IsPastTargetTimeOfDay(nowLocal.TimeOfDay))
        {
            return new AgentConditionDigestRunResult(AgentConditionDigestOutcome.NotDueYet, 0);
        }

        var claimed = await _settingsRepository.TryAdvanceSettingAsync(
            Settings.AGENT_CONDITION_DIGEST_LAST_RUN_DATE, lastRunValue, todayKey);
        if (!claimed)
        {
            return new AgentConditionDigestRunResult(AgentConditionDigestOutcome.LostRace, 0);
        }

        var recipientsNotified = await BuildAndDispatchDigestsAsync(
            DateOnly.FromDateTime(nowLocal.Date), nowUtc, cancellationToken);

        return new AgentConditionDigestRunResult(AgentConditionDigestOutcome.Ran, recipientsNotified);
    }

    /// <summary>
    /// Reads the persisted watermark, seeding it once - as the CAS-comparable "never ran" value - when a
    /// fresh installation has no row yet. Without this, TryAdvanceSettingAsync's WHERE clause would never
    /// match any row and the digest would never fire, silently, forever. A unique-constraint collision
    /// from a second instance seeding concurrently is not an error: it means the row now exists, so the
    /// caller just re-reads it.
    /// </summary>
    private async Task<string> ReadOrSeedLastRunMarkerAsync(CancellationToken cancellationToken)
    {
        var existing = await _settingsRepository.GetSettingNoTracking(Settings.AGENT_CONDITION_DIGEST_LAST_RUN_DATE);
        if (existing is not null)
        {
            return existing.Value;
        }

        try
        {
            await _settingsRepository.AddSetting(new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = Settings.AGENT_CONDITION_DIGEST_LAST_RUN_DATE,
                Value = NeverRanMarker
            });
            await _unitOfWork.CompleteAsync();
            return NeverRanMarker;
        }
        catch (DbUpdateException)
        {
            var seededByAnotherInstance = await _settingsRepository.GetSettingNoTracking(Settings.AGENT_CONDITION_DIGEST_LAST_RUN_DATE);
            return seededByAnotherInstance?.Value ?? NeverRanMarker;
        }
    }

    private bool IsPastTargetTimeOfDay(TimeSpan nowLocalTimeOfDay)
    {
        var configured = _options.AgentConditionDigestTimeOfDayLocal;
        if (TimeSpan.TryParse(configured, CultureInfo.InvariantCulture, out var target)
            && target >= TimeSpan.Zero && target < TimeSpan.FromDays(1))
        {
            return nowLocalTimeOfDay >= target;
        }

        _logger.LogWarning(
            "Daily digest: invalid AgentConditionDigestTimeOfDayLocal '{Value}', falling back to {Default}",
            configured, AgentConditionDigestDefaults.DefaultTimeOfDayLocal);

        return nowLocalTimeOfDay >= FallbackTimeOfDay;
    }

    private async Task<TimeZoneInfo> ResolveTimeZoneAsync()
    {
        var explicitSetting = await _settingsRepository.GetSetting(Settings.APP_ADDRESS_TIMEZONE);
        if (TryGetTimeZone(explicitSetting?.Value, out var explicitZone))
        {
            return explicitZone!;
        }

        var countrySetting = await _settingsRepository.GetSetting(Settings.APP_ADDRESS_COUNTRY);
        if (TryGetTimeZone(CountryTimeZones.Resolve(countrySetting?.Value), out var countryZone))
        {
            return countryZone!;
        }

        return TimeZoneInfo.Utc;
    }

    private static bool TryGetTimeZone(string? timeZoneId, out TimeZoneInfo? zone)
    {
        zone = null;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }

    private async Task<int> BuildAndDispatchDigestsAsync(DateOnly localDigestDate, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var plannerIds = await _planningAudienceResolver.GetPlanningUserIdsAsync(cancellationToken);
        if (plannerIds.Count == 0)
        {
            _logger.LogInformation("Daily digest: no planners in the audience, nothing to send");
            return 0;
        }

        var newCutoffUtc = nowUtc.AddHours(-AgentConditionDigestDefaults.NewWithinHours);
        var notified = 0;

        foreach (var plannerIdText in plannerIds)
        {
            if (!Guid.TryParse(plannerIdText, out var plannerId))
            {
                _logger.LogWarning("Daily digest: planner id '{PlannerId}' is not a GUID, skipped", plannerIdText);
                continue;
            }

            try
            {
                if (await TryDispatchOneDigestAsync(plannerId, plannerIdText, localDigestDate, newCutoffUtc, cancellationToken))
                {
                    notified++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Daily digest: build/dispatch failed for planner {PlannerId}, skipped for today", plannerId);
            }
        }

        _logger.LogInformation("Daily digest dispatched to {Count} planner(s) for {Date}", notified, localDigestDate);
        return notified;
    }

    /// <summary>
    /// Builds and dispatches one planner's digest. Isolated per planner so that one planner's scope
    /// throwing (transient DB error, broken GroupVisibility row) does not cost every other planner in the
    /// same run their digest for the day - the watermark is already claimed before this loop runs, so a
    /// loop-wide abort would silently skip the remaining recipients until tomorrow.
    /// </summary>
    private async Task<bool> TryDispatchOneDigestAsync(
        Guid plannerId,
        string plannerIdText,
        DateOnly localDigestDate,
        DateTime newCutoffUtc,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeResolver.ResolveAsync(plannerIdText, cancellationToken);
        if (!scope.IsPlanner)
        {
            return false;
        }

        var visible = await _conditionRepository.GetOpenForScopeAsync(
            scope.IsUnrestricted, scope.VisibleRootIds, AgentConditionDigestDefaults.ScopeQueryCap, cancellationToken);

        if (visible.Count == 0)
        {
            return false;
        }

        var totalCount = visible.Count;
        if (visible.Count >= AgentConditionDigestDefaults.ScopeQueryCap)
        {
            totalCount = await _conditionRepository.CountOpenForScopeAsync(
                scope.IsUnrestricted, scope.VisibleRootIds, cancellationToken);
            _logger.LogWarning(
                "Daily digest: planner {PlannerId} scope hit the {Cap}-row query cap ({True} truly open) - severity breakdown reflects only the capped sample",
                plannerId, AgentConditionDigestDefaults.ScopeQueryCap, totalCount);
        }

        var digestEvent = BuildDigestEvent(plannerId, localDigestDate, totalCount, visible, newCutoffUtc);
        await _triggerService.OnEventAsync(digestEvent, cancellationToken);
        return true;
    }

    /// <summary>
    /// <paramref name="visible"/> arrives already sorted severity-then-age by GetOpenForScopeAsync, so
    /// its first TopFindingsCount rows are exactly the top-priority short list without a second sort.
    /// </summary>
    private static AgentConditionDigestTriggerEvent BuildDigestEvent(
        Guid plannerId,
        DateOnly localDigestDate,
        int totalCount,
        List<AgentCondition> visible,
        DateTime newCutoffUtc)
    {
        var highCount = visible.Count(c => c.Severity == AgentTriggerSeverity.High);
        var mediumCount = visible.Count(c => c.Severity == AgentTriggerSeverity.Medium);
        var lowCount = visible.Count(c => c.Severity == AgentTriggerSeverity.Low);
        var newCount = visible.Count(c => c.DetectedAtUtc >= newCutoffUtc);

        var topFindings = visible
            .Take(AgentConditionDigestDefaults.TopFindingsCount)
            .Select(c => new AgentConditionDigestFinding(c.TriggerKind, c.EntityId, c.GroupId, c.Severity, c.DetectedAtUtc))
            .ToList();

        return new AgentConditionDigestTriggerEvent(
            plannerId, localDigestDate, totalCount, highCount, mediumCount, lowCount, newCount, topFindings);
    }
}
