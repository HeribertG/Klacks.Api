// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ICompensatoryRestObligationReconciler"/> (K12 stage 1). Loads the client's Work
/// rows over the effective window, rebuilds ScheduleBlocks through the SAME
/// <see cref="ITimelineCalculationService"/> the live validator uses (so obligation and feed warning can
/// never diverge — WorkChange corrections/replacements are folded in identically), derives rest-shortfall
/// gaps via <see cref="ClientTimeline.GetRestViolations"/>, and upserts one obligation per gap keyed by
/// (ClientId, RestGapStart). StandardRestHours is snapshotted from the policy at creation and never
/// recomputed; on re-reconcile ShortfallHours is recomputed against that snapshot (never against the live
/// policy), so the fulfilment threshold StandardRestHours + ShortfallHours is composed only of
/// snapshot-anchored values and stays stable when the policy changes later. Fulfilment = a strictly later
/// rest gap of at least that threshold that begins on or before the deadline; its date is stored so the
/// pass is idempotent. A repaired triggering gap soft-deletes the open obligation.
/// STALENESS BOUND: obligation state is only refreshed when a reconcile runs - on a live edit
/// (ScheduleTimelineBackgroundService) or on a period-closing load (PeriodValidationLoader). A period
/// never re-edited after a shortfall keeps its last reconciled state until the next close-load re-runs
/// this pass, which is why that load reconciles before it reads (see PeriodValidationLoader remarks).
/// </summary>
/// <param name="context">Reads the client's Work/WorkChange rows and owns the SaveChanges boundary</param>
/// <param name="obligationRepository">Reads and mutates CompensatoryRestObligation rows on the shared context</param>
/// <param name="policyResolver">Resolves MinRestHours used to detect new shortfall gaps</param>
/// <param name="timelineCalculationService">Shared schedule-block builder used by the live validator</param>
/// <param name="settingsReader">Reads the enabled flag and the deadline-days setting</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class CompensatoryRestObligationReconciler : ICompensatoryRestObligationReconciler
{
    private const string TrueValue = "true";

    private readonly DataBaseContext _context;
    private readonly ICompensatoryRestObligationRepository _obligationRepository;
    private readonly ISchedulingPolicyResolver _policyResolver;
    private readonly ITimelineCalculationService _timelineCalculationService;
    private readonly ISettingsReader _settingsReader;

    public CompensatoryRestObligationReconciler(
        DataBaseContext context,
        ICompensatoryRestObligationRepository obligationRepository,
        ISchedulingPolicyResolver policyResolver,
        ITimelineCalculationService timelineCalculationService,
        ISettingsReader settingsReader)
    {
        _context = context;
        _obligationRepository = obligationRepository;
        _policyResolver = policyResolver;
        _timelineCalculationService = timelineCalculationService;
        _settingsReader = settingsReader;
    }

    public async Task ReconcileAsync(
        Guid clientId,
        DateOnly windowStart,
        DateOnly windowEnd,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (analyseToken is not null)
        {
            return;
        }

        if (!await IsEnabledAsync())
        {
            return;
        }

        var deadlineDays = await ResolveDeadlineDaysAsync();
        if (deadlineDays <= 0)
        {
            return;
        }

        var existing = await _obligationRepository.GetActiveByClientInWindowAsync(clientId, windowStart, windowEnd);
        var openExisting = existing.Where(o => o.FulfilledOn == null).ToList();

        var effectiveStart = windowStart;
        var effectiveEnd = windowEnd;
        foreach (var o in openExisting)
        {
            if (o.TriggerDate < effectiveStart)
            {
                effectiveStart = o.TriggerDate;
            }

            if (o.DueDate > effectiveEnd)
            {
                effectiveEnd = o.DueDate;
            }
        }

        var timeline = await BuildTimelineAsync(clientId, effectiveStart, effectiveEnd, cancellationToken);
        var policy = await _policyResolver.GetForClientAsync(clientId, windowStart);

        var violationsByKey = timeline
            .GetRestViolations(policy.MinRestHours)
            .GroupBy(v => v.PreviousBlock.End)
            .ToDictionary(g => g.Key, g => g.First());

        var gaps = BuildGaps(timeline);
        var existingByKey = existing
            .GroupBy(o => o.RestGapStart)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var (restGapStart, violation) in violationsByKey)
        {
            if (existingByKey.ContainsKey(restGapStart))
            {
                continue;
            }

            var triggerDate = violation.PreviousBlock.OwnerDate;

            // The timeline spans the effective window (extended forward to cover open obligations'
            // deadlines for the fulfilment scan), but a new obligation must only be CREATED for a trigger
            // inside the CHECK window. A trigger in the extended region belongs to an obligation that was
            // created by the reconcile of its own window; re-creating it here would collide with the
            // (ClientId, RestGapStart) partial unique index.
            if (triggerDate < windowStart || triggerDate > windowEnd)
            {
                continue;
            }

            var dueDate = triggerDate.AddDays(deadlineDays);
            var standard = (decimal)violation.RequiredRest.TotalHours;
            var shortfall = (decimal)(violation.RequiredRest - violation.ActualRest).TotalHours;
            var obligation = new CompensatoryRestObligation
            {
                ClientId = clientId,
                TriggerDate = triggerDate,
                // Stored as timestamp without time zone; normalise the Kind so the DstAware path (blocks
                // carry Kind=Utc) does not fail on write. The value is an opaque natural key, so equality
                // still matches (DateTime equality ignores Kind).
                RestGapStart = DateTime.SpecifyKind(restGapStart, DateTimeKind.Unspecified),
                StandardRestHours = standard,
                ShortfallHours = shortfall,
                DueDate = dueDate,
                FulfilledOn = FindFulfillment(gaps, restGapStart, dueDate, standard + shortfall),
            };
            _obligationRepository.Add(obligation);
        }

        foreach (var obligation in openExisting)
        {
            var currentGap = gaps.FirstOrDefault(g => g.GapStart == obligation.RestGapStart);
            var stillViolates = currentGap.GapStart == obligation.RestGapStart
                && currentGap.Duration < TimeSpan.FromHours((double)obligation.StandardRestHours);

            if (stillViolates)
            {
                obligation.ShortfallHours = obligation.StandardRestHours - (decimal)currentGap.Duration.TotalHours;
            }

            var threshold = obligation.StandardRestHours + obligation.ShortfallHours;
            var fulfilledOn = FindFulfillment(gaps, obligation.RestGapStart, obligation.DueDate, threshold);

            if (fulfilledOn is not null)
            {
                obligation.FulfilledOn = fulfilledOn;
                _obligationRepository.Update(obligation);
            }
            else if (!stillViolates)
            {
                _obligationRepository.Delete(obligation);
            }
            else
            {
                _obligationRepository.Update(obligation);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<ClientTimeline> BuildTimelineAsync(
        Guid clientId,
        DateOnly effectiveStart,
        DateOnly effectiveEnd,
        CancellationToken cancellationToken)
    {
        // A Work anchored the day before the window can end inside it (cross-midnight), so load one extra
        // leading day to keep such a preceding block available for the rest-gap computation.
        var loadStart = effectiveStart.AddDays(-1);
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId
                && !w.IsDeleted
                && w.ParentWorkId == null
                && w.AnalyseToken == null
                && w.CurrentDate >= loadStart
                && w.CurrentDate <= effectiveEnd)
            .ToListAsync(cancellationToken);

        var workIds = works.Select(w => w.Id).ToList();
        var workChanges = workIds.Count > 0
            ? await _context.WorkChange
                .AsNoTracking()
                .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
                .ToListAsync(cancellationToken)
            : [];

        var blocks = _timelineCalculationService.CalculateScheduleBlocks(works, workChanges, []);
        var timeline = new ClientTimeline(clientId);
        timeline.AddBlocks(blocks.Where(b => b.ClientId == clientId));
        timeline.SortBlocks();
        return timeline;
    }

    private static List<(DateTime GapStart, TimeSpan Duration)> BuildGaps(ClientTimeline timeline)
    {
        var workBlocks = timeline.Blocks
            .Where(b => b.BlockType == ScheduleBlockType.Work)
            .OrderBy(b => b.Start)
            .ToList();

        var gaps = new List<(DateTime GapStart, TimeSpan Duration)>();
        for (var i = 0; i < workBlocks.Count - 1; i++)
        {
            var gap = workBlocks[i + 1].Start - workBlocks[i].End;
            if (gap >= TimeSpan.Zero)
            {
                gaps.Add((workBlocks[i].End, gap));
            }
        }

        return gaps;
    }

    // A fulfilling compensatory rest is a STRICTLY later gap (so repairing the triggering gap itself does
    // not count) that begins on or before the deadline and lasts at least the snapshot-anchored threshold.
    private static DateOnly? FindFulfillment(
        IReadOnlyList<(DateTime GapStart, TimeSpan Duration)> gaps,
        DateTime restGapStart,
        DateOnly dueDate,
        decimal thresholdHours)
    {
        var required = TimeSpan.FromHours((double)thresholdHours);
        foreach (var gap in gaps)
        {
            if (gap.GapStart <= restGapStart)
            {
                continue;
            }

            if (DateOnly.FromDateTime(gap.GapStart) > dueDate)
            {
                continue;
            }

            if (gap.Duration >= required)
            {
                return DateOnly.FromDateTime(gap.GapStart);
            }
        }

        return null;
    }

    private async Task<bool> IsEnabledAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.ComplianceCompensatoryRestEnabled);
        return string.Equals(setting?.Value, TrueValue, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<int> ResolveDeadlineDaysAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.ComplianceCompensatoryRestDeadlineDays);
        return int.TryParse(setting?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days)
            ? days
            : 0;
    }
}
