// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IRestDayRotationEvaluator"/> (K10). For every active rule and every candidate
/// date, the trailing window ending on the most recent occurrence of the rule's weekday is inspected:
/// it spans exactly WindowWeeks occurrences of that weekday, and each occurrence counts as work-free
/// when no Work row touches it. Runs as a direct database check over the full window (the
/// PeriodCapEvaluator pattern) because the window (typically 4-26 weeks) far exceeds the +-2-week
/// timeline the ScheduleValidationBuilder callers load. Warning escalates to Error when the
/// restDayRotation compliance rule's enforcement mode is Block.
/// </summary>
/// <param name="ruleRepository">Reads the active RestDayRotationRule set</param>
/// <param name="context">Database access for the client's Work rows in the window</param>
/// <param name="enforcementResolver">Resolves warn/block for the restDayRotation compliance rule</param>
/// <param name="membershipStartResolver">Resolves a client's employment start date for the window clamp</param>
/// <param name="contractDataProvider">Resolves the client's active SchedulingRule for industry-scoped rules</param>
/// <remarks>
/// Occupancy uses the shared rest-day definition (CalendarWeekRestDays, owner rule 2026-09-30): a Work
/// occupies the day it starts on; the morning end of a cross-midnight Work occupies the following day
/// unless the day after that is free and the free block reaches the package rest of the client's
/// MinRestDays - so a Saturday night shift followed by a free Monday no longer kills the Sunday. Works
/// are loaded beyond the window end far enough to know that next shift. Breaks (vacation/sickness) are
/// ignored: an absence is not a working day, so an absent Sunday counts as free - the same convention
/// ClientTimeline.GetRestDayCount documents. Industry-scoped rules and MinRestDays are resolved once per
/// evaluation at the latest candidate date. WorkChange corrections are not folded in (Work rows are
/// the planning source of truth this rule governs). A window that starts before the client's
/// Membership.ValidFrom is skipped entirely rather than evaluated pro-rata: a minimum COUNT over a
/// shortened window has no honest proportional reading, and skipping is the direction that never
/// fabricates a violation for a recent starter.
/// </remarks>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Common.RestDays;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class RestDayRotationEvaluator : IRestDayRotationEvaluator
{
    private const int DaysPerWeek = 7;

    private const int LookAheadPaddingDays = 2;

    private readonly IRestDayRotationRuleRepository _ruleRepository;
    private readonly DataBaseContext _context;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly IClientMembershipStartResolver _membershipStartResolver;
    private readonly IClientContractDataProvider _contractDataProvider;

    public RestDayRotationEvaluator(
        IRestDayRotationRuleRepository ruleRepository,
        DataBaseContext context,
        IComplianceEnforcementResolver enforcementResolver,
        IClientMembershipStartResolver membershipStartResolver,
        IClientContractDataProvider contractDataProvider)
    {
        _ruleRepository = ruleRepository;
        _context = context;
        _enforcementResolver = enforcementResolver;
        _membershipStartResolver = membershipStartResolver;
        _contractDataProvider = contractDataProvider;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        return await EvaluateCoreAsync(clientId, clientName, [asOfDate], [], analyseToken, cancellationToken);
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)> plannedSlots,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (plannedSlots.Count == 0)
        {
            return [];
        }

        var candidateDates = plannedSlots.Select(s => s.Date).Distinct().ToList();
        var plannedWorks = plannedSlots
            .Select(slot => ToInterval(slot.Date, slot.StartTime, slot.EndTime))
            .ToList();

        return await EvaluateCoreAsync(clientId, clientName, candidateDates, plannedWorks, analyseToken, cancellationToken);
    }

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateCoreAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<DateOnly> candidateDates,
        IReadOnlyList<WorkInterval> plannedWorks,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var asOfDate = candidateDates.Max();
        var allRules = await _ruleRepository.GetAllActiveAsync();
        if (allRules.Count == 0)
        {
            return [];
        }

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, asOfDate);
        var rules = allRules
            .Where(r => r.SchedulingRuleId == null || r.SchedulingRuleId == effectiveData.SchedulingRuleId)
            .ToList();
        if (rules.Count == 0)
        {
            return [];
        }

        var minimumRestDays = effectiveData.MinRestDays > 0 ? effectiveData.MinRestDays : SchedulingPolicyDefaults.MinRestDays;
        var minimumFreeBlock = CalendarWeekRestDays.MinimumFreeBlock(minimumRestDays);
        var lookAheadDays = (int)Math.Ceiling(minimumFreeBlock.TotalDays) + LookAheadPaddingDays;

        var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.RestDayRotation);
        var membershipStart = await _membershipStartResolver.GetValidFromAsync(clientId);

        var entries = new List<ScheduleValidationNotificationDto>();
        var reportedWindows = new HashSet<(Guid RuleId, DateOnly WindowEnd)>();

        // A Work anchored on a candidate date can occupy BOTH that date and the following day
        // (cross-midnight), so the window anchored at the next day must be inspected as well - otherwise
        // a Saturday 22:00-07:00 placement would only ever check the window ending on the PREVIOUS
        // Sunday and the Sunday it actually occupies would never be an inspected occurrence.
        var anchorDates = candidateDates
            .SelectMany(date => new[] { date, date.AddDays(1) })
            .Distinct()
            .ToList();

        foreach (var rule in rules)
        {
            foreach (var date in anchorDates)
            {
                var windowEnd = MostRecentOccurrenceOnOrBefore(rule.DayOfWeek, date);
                var windowStart = windowEnd.AddDays(-DaysPerWeek * (rule.WindowWeeks - 1));
                if (membershipStart.HasValue && membershipStart.Value > windowStart)
                {
                    continue;
                }

                if (!reportedWindows.Add((rule.Id, windowEnd)))
                {
                    continue;
                }

                var works = await LoadWorksAsync(
                    clientId, windowStart.AddDays(-1), windowEnd.AddDays(lookAheadDays), analyseToken, cancellationToken);
                works.AddRange(plannedWorks);

                var freeCount = 0;
                for (var occurrence = windowStart; occurrence <= windowEnd; occurrence = occurrence.AddDays(DaysPerWeek))
                {
                    if (!CalendarWeekRestDays.IsWorkDay(occurrence, works, minimumFreeBlock))
                    {
                        freeCount++;
                    }
                }

                if (freeCount >= rule.MinFreeCount)
                {
                    continue;
                }

                entries.Add(BuildEntry(clientId, clientName, windowEnd, rule, freeCount, mode));
            }
        }

        return entries;
    }

    private async Task<List<WorkInterval>> LoadWorksAsync(
        Guid clientId,
        DateOnly loadStart,
        DateOnly loadEnd,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId
                && !w.IsDeleted
                && w.AnalyseToken == analyseToken
                && w.CurrentDate >= loadStart
                && w.CurrentDate <= loadEnd)
            .Select(w => new { w.CurrentDate, w.StartTime, w.EndTime })
            .ToListAsync(cancellationToken);

        return works.Select(work => ToInterval(work.CurrentDate, work.StartTime, work.EndTime)).ToList();
    }

    private static WorkInterval ToInterval(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var start = date.ToDateTime(startTime);
        var end = endTime <= startTime ? date.AddDays(1).ToDateTime(endTime) : date.ToDateTime(endTime);
        return new WorkInterval(start, end);
    }

    private static DateOnly MostRecentOccurrenceOnOrBefore(DayOfWeek dayOfWeek, DateOnly date)
    {
        var delta = ((int)date.DayOfWeek - (int)dayOfWeek + DaysPerWeek) % DaysPerWeek;
        return date.AddDays(-delta);
    }

    private static ScheduleValidationNotificationDto BuildEntry(
        Guid clientId,
        string clientName,
        DateOnly windowEnd,
        RestDayRotationRule rule,
        int freeCount,
        RuleEnforcementMode mode)
    {
        var isBlocked = mode == RuleEnforcementMode.Block;
        var commentParams = new Dictionary<string, string>
        {
            ["dayOfWeek"] = rule.DayOfWeek.ToString(),
            ["actualFree"] = freeCount.ToString(CultureInfo.InvariantCulture),
            ["minFree"] = rule.MinFreeCount.ToString(CultureInfo.InvariantCulture),
            ["windowWeeks"] = rule.WindowWeeks.ToString(CultureInfo.InvariantCulture),
        };
        if (isBlocked)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.RestDayRotation;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isBlocked ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = windowEnd,
            Comment = ScheduleValidationKeys.RestDayRotation,
            CommentParams = commentParams,
        };
    }
}
