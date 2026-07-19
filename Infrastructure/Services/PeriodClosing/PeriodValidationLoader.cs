// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads validation findings (rest, overtime, consecutive days, collisions) for a billing
/// period by replaying the same timeline computation the SignalR background validator runs.
/// Used by the period-closing Issues card so the user sees the real problems without
/// having to subscribe to live notifications.
/// CONSTRAINT (deliberate CQRS side effect in a query path): the K12 compensatory-rest state is
/// materialised (persisted obligations), and the live reconcile only runs on editing. A period never
/// re-edited after a shortfall would therefore keep a stale obligation open/overdue - and under
/// enforcement=block that would wrongly block the close of a situation that has long been compensated
/// or repaired. So this loader reconciles each client's obligations for [from, to] BEFORE evaluating
/// them, guaranteeing the compensatory-rest state is fresh at close time. The reconcile is real-mode
/// only (it no-ops on a non-null analyseToken), so scenario loads stay side-effect-free.
/// </summary>
/// <param name="context">Read-only access to Work/WorkChange/Break/GroupItem for the period</param>
/// <param name="timelineCalculator">Shared schedule-block builder used by the live validator</param>
/// <param name="policyResolver">Resolves rest/overtime/consecutive-day thresholds per client</param>
/// <param name="periodCapEvaluator">Reports a K5 period-cap breach for the period being closed</param>
/// <param name="compensatoryRestReconciler">Refreshes K12 obligation state before the load reads it</param>
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.PeriodClosing;

public class PeriodValidationLoader : IPeriodValidationLoader
{
    private const int MaxIssues = 500;

    private readonly DataBaseContext _context;
    private readonly ITimelineCalculationService _timelineCalculator;
    private readonly ISchedulingPolicyResolver _policyResolver;
    private readonly IPeriodCapEvaluator _periodCapEvaluator;
    private readonly IRestDayRotationEvaluator _restDayRotationEvaluator;
    private readonly ICounterRuleEvaluator _counterRuleEvaluator;
    private readonly IRestrictedTimeWindowEvaluator _restrictedTimeWindowEvaluator;
    private readonly ICompensatoryRestObligationReconciler _compensatoryRestReconciler;
    private readonly ICompensatoryRestEvaluator _compensatoryRestEvaluator;

    public PeriodValidationLoader(
        DataBaseContext context,
        ITimelineCalculationService timelineCalculator,
        ISchedulingPolicyResolver policyResolver,
        IPeriodCapEvaluator periodCapEvaluator,
        IRestDayRotationEvaluator restDayRotationEvaluator,
        ICounterRuleEvaluator counterRuleEvaluator,
        IRestrictedTimeWindowEvaluator restrictedTimeWindowEvaluator,
        ICompensatoryRestObligationReconciler compensatoryRestReconciler,
        ICompensatoryRestEvaluator compensatoryRestEvaluator)
    {
        _context = context;
        _timelineCalculator = timelineCalculator;
        _policyResolver = policyResolver;
        _periodCapEvaluator = periodCapEvaluator;
        _restDayRotationEvaluator = restDayRotationEvaluator;
        _counterRuleEvaluator = counterRuleEvaluator;
        _restrictedTimeWindowEvaluator = restrictedTimeWindowEvaluator;
        _compensatoryRestReconciler = compensatoryRestReconciler;
        _compensatoryRestEvaluator = compensatoryRestEvaluator;
    }

    public async Task<List<PeriodIssueDto>> LoadAsync(
        DateOnly from,
        DateOnly to,
        Guid? groupId,
        Guid? analyseToken = null,
        int? maxIssues = null,
        CancellationToken cancellationToken = default)
    {
        var issueLimit = maxIssues ?? MaxIssues;
        var clientIdsInGroup = groupId.HasValue
            ? await LoadClientIdsForGroupAsync(groupId.Value, cancellationToken)
            : null;

        var works = await LoadWorksAsync(from, to, clientIdsInGroup, analyseToken, cancellationToken);
        if (works.Count == 0)
        {
            return [];
        }

        var workIds = works.Select(w => w.Id).ToList();
        var workChanges = await LoadWorkChangesAsync(workIds, cancellationToken);
        var breaks = await LoadBreaksAsync(from, to, clientIdsInGroup, analyseToken, cancellationToken);

        var clientNameLookup = BuildClientNameLookup(works, workChanges);
        var scheduleBlocks = _timelineCalculator.CalculateScheduleBlocks(works, workChanges, breaks);

        var grouped = scheduleBlocks.GroupBy(b => b.ClientId).ToList();
        if (grouped.Count == 0)
        {
            return [];
        }

        var clientIds = grouped.Select(g => g.Key).ToList();
        var policies = await _policyResolver.GetForClientsAsync(clientIds, from);

        var entries = new List<ScheduleValidationNotificationDto>();
        foreach (var group in grouped)
        {
            var timeline = new ClientTimeline(group.Key);
            timeline.AddBlocks(group);
            timeline.SortBlocks();

            clientNameLookup.TryGetValue(group.Key, out var clientName);
            clientName ??= string.Empty;

            var policy = policies.TryGetValue(group.Key, out var found)
                ? found
                : await _policyResolver.GetForClientAsync(group.Key, from);

            ScheduleValidationBuilder.AddCollisions(entries, timeline, clientName);
            ScheduleValidationBuilder.AddRestViolations(entries, timeline, clientName, policy);
            ScheduleValidationBuilder.AddOvertime(entries, timeline, clientName, from, to, policy);
            ScheduleValidationBuilder.AddConsecutiveDays(entries, timeline, clientName, from, to, policy);
            ScheduleValidationBuilder.AddWeeklyOvertime(entries, timeline, clientName, from, to, policy);
            ScheduleValidationBuilder.AddMinRestDays(entries, timeline, clientName, from, to, policy);

            entries.AddRange(await _periodCapEvaluator.EvaluateAsync(group.Key, clientName, from, analyseToken, cancellationToken));
            entries.AddRange(await _restDayRotationEvaluator.EvaluateAsync(group.Key, clientName, to, analyseToken, cancellationToken));
            entries.AddRange(await _counterRuleEvaluator.EvaluateAsync(group.Key, clientName, to, analyseToken, cancellationToken));
            entries.AddRange(await _restrictedTimeWindowEvaluator.EvaluateRangeAsync(group.Key, clientName, from, to, analyseToken, cancellationToken));

            // Refresh materialised K12 state before reading it, so a period never re-edited after a
            // shortfall does not surface an obligation that was already compensated or repaired.
            await _compensatoryRestReconciler.ReconcileAsync(group.Key, from, to, analyseToken, cancellationToken);
            entries.AddRange(await _compensatoryRestEvaluator.EvaluateAsync(group.Key, clientName, to, analyseToken, cancellationToken));
        }

        return entries
            .OrderBy(e => e.Date)
            .ThenBy(e => e.ClientName)
            .Take(issueLimit)
            .Select(ToDto)
            .ToList();
    }

    private async Task<HashSet<Guid>> LoadClientIdsForGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var ids = await _context.GroupItem
            .AsNoTracking()
            .Where(gi => !gi.IsDeleted && gi.GroupId == groupId && gi.ClientId != null)
            .Select(gi => gi.ClientId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        return new HashSet<Guid>(ids);
    }

    private async Task<List<Work>> LoadWorksAsync(
        DateOnly from,
        DateOnly to,
        HashSet<Guid>? clientFilter,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var query = _context.Work
            .AsNoTracking()
            .Include(w => w.Client)
            .Where(w => w.CurrentDate >= from
                && w.CurrentDate <= to
                && !w.IsDeleted
                && w.ParentWorkId == null
                && w.AnalyseToken == analyseToken);

        if (clientFilter != null)
        {
            query = query.Where(w => clientFilter.Contains(w.ClientId));
        }

        return await query.ToListAsync(cancellationToken);
    }

    private async Task<List<WorkChange>> LoadWorkChangesAsync(
        List<Guid> workIds,
        CancellationToken cancellationToken)
    {
        if (workIds.Count == 0)
        {
            return [];
        }

        return await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.ReplaceClient)
            .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Break>> LoadBreaksAsync(
        DateOnly from,
        DateOnly to,
        HashSet<Guid>? clientFilter,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var query = _context.Break
            .AsNoTracking()
            .Where(b => b.CurrentDate >= from
                && b.CurrentDate <= to
                && !b.IsDeleted
                && b.ParentWorkId == null
                && b.AnalyseToken == analyseToken);

        if (clientFilter != null)
        {
            query = query.Where(b => clientFilter.Contains(b.ClientId));
        }

        return await query.ToListAsync(cancellationToken);
    }

    private static Dictionary<Guid, string> BuildClientNameLookup(List<Work> works, List<WorkChange> workChanges)
    {
        var lookup = new Dictionary<Guid, string>();
        foreach (var work in works)
        {
            if (work.Client != null && !lookup.ContainsKey(work.ClientId))
            {
                lookup[work.ClientId] = $"{work.Client.Name} {work.Client.FirstName}".Trim();
            }
        }
        foreach (var wc in workChanges)
        {
            if (wc.ReplaceClient != null && wc.ReplaceClientId.HasValue && !lookup.ContainsKey(wc.ReplaceClientId.Value))
            {
                lookup[wc.ReplaceClientId.Value] = $"{wc.ReplaceClient.Name} {wc.ReplaceClient.FirstName}".Trim();
            }
        }
        return lookup;
    }

    private static PeriodIssueDto ToDto(ScheduleValidationNotificationDto entry)
    {
        return new PeriodIssueDto
        {
            Date = entry.Date,
            ClientId = entry.ClientId,
            ClientName = entry.ClientName,
            Severity = entry.Type,
            Code = MapCode(entry.Comment),
            MessageKey = entry.Comment,
            MessageParams = new Dictionary<string, string>(entry.CommentParams)
        };
    }

    private static string MapCode(string comment) => comment switch
    {
        "schedule.error-list.collision" => "Collision",
        "schedule.error-list.rest-violation" => "RestViolation",
        "schedule.error-list.overtime" => "Overtime",
        "schedule.error-list.consecutive-days" => "ConsecutiveDays",
        "schedule.error-list.weekly-overtime" => "WeeklyOvertime",
        "schedule.error-list.min-rest-days" => "MinRestDays",
        "schedule.error-list.period-cap" => "PeriodCap",
        "schedule.error-list.rolling-average" => "RollingAverage",
        "schedule.error-list.rest-day-rotation" => "RestDayRotation",
        "schedule.error-list.counter-rule" => "CounterRule",
        "schedule.error-list.restricted-time-window" => "RestrictedTimeWindow",
        "schedule.error-list.compensatory-rest-due" => "CompensatoryRestDue",
        "schedule.error-list.compensatory-rest-overdue" => "CompensatoryRestOverdue",
        _ => "ScheduleValidation"
    };
}
