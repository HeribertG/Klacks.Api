// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IRestrictedTimeWindowEvaluator"/> (K16). Every candidate work (persisted on the
/// as-of date, or the not-yet-saved planned slots) is tested against every active rule: the work's shift
/// must be in the rule's group-tag scope, and the work interval must overlap the rule's daily forbidden
/// window on a calendar day inside the season. Runs as a direct database check (the PeriodCapEvaluator
/// pattern). Warning escalates to Error when the restrictedTimeWindow enforcement mode is Block.
/// </summary>
/// <param name="ruleRepository">Reads the active RestrictedTimeWindowRule set</param>
/// <param name="context">Database access for the client's Work rows and the group-tag scoping join</param>
/// <param name="enforcementResolver">Resolves warn/block for the restrictedTimeWindow compliance rule</param>
/// <remarks>
/// Group-tag scoping reads GroupItem/Group by SHIFT ID with NO AnalyseToken filter, exactly as the wizard
/// qualification eligibility does: a scenario clones a shift under a fresh id and clones its group links to
/// that clone id, so a cloned work finds the cloned link and a real work finds the real link - the id
/// spaces are disjoint, so an unfiltered query is correct in both worlds. An empty AppliesToGroupTag matches every
/// shift (including works with no ShiftId). The season/daily-window arithmetic in <see cref="WindowBlocks"/>
/// is duplicated on the optimizer side (CoreRestrictedTimeWindow.WouldBlock); the two copies are pinned by
/// a single shared vector table in the unit tests. GroupItem carries the K16 soft-delete filter per the
/// stored-procedures rule; a soft-deleted link removes the shift from scope. A cross-midnight work that
/// overlaps the NEXT day's window is deliberately reported on its own anchor date (where the work is
/// planned), not on the day the window falls - the violation belongs to the shift the user placed.
/// </remarks>

using System.Globalization;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class RestrictedTimeWindowEvaluator : IRestrictedTimeWindowEvaluator
{
    private const int MinutesPerDay = 24 * 60;

    private readonly IRestrictedTimeWindowRuleRepository _ruleRepository;
    private readonly DataBaseContext _context;
    private readonly IComplianceEnforcementResolver _enforcementResolver;

    public RestrictedTimeWindowEvaluator(
        IRestrictedTimeWindowRuleRepository ruleRepository,
        DataBaseContext context,
        IComplianceEnforcementResolver enforcementResolver)
    {
        _ruleRepository = ruleRepository;
        _context = context;
        _enforcementResolver = enforcementResolver;
    }

    private readonly record struct CandidateWork(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, Guid? ShiftId);

    public Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        return EvaluateRangeAsync(clientId, clientName, asOfDate, asOfDate, analyseToken, cancellationToken);
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluateRangeAsync(
        Guid clientId,
        string clientName,
        DateOnly from,
        DateOnly to,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        var works = await LoadWorksAsync(clientId, from, to, analyseToken, cancellationToken);
        return await EvaluateCoreAsync(clientId, clientName, works, cancellationToken);
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, Guid? ShiftId)> plannedSlots,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (plannedSlots.Count == 0)
        {
            return [];
        }

        var works = plannedSlots
            .Select(s => new CandidateWork(s.Date, s.StartTime, s.EndTime, s.ShiftId))
            .ToList();
        return await EvaluateCoreAsync(clientId, clientName, works, cancellationToken);
    }

    private async Task<List<ScheduleValidationNotificationDto>> EvaluateCoreAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<CandidateWork> works,
        CancellationToken cancellationToken)
    {
        if (works.Count == 0)
        {
            return [];
        }

        var rules = await _ruleRepository.GetAllActiveAsync();
        if (rules.Count == 0)
        {
            return [];
        }

        var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.RestrictedTimeWindow);
        var tagsByShift = await LoadGroupTagsByShiftAsync(works, cancellationToken);

        var entries = new List<ScheduleValidationNotificationDto>();
        var reported = new HashSet<(Guid RuleId, DateOnly Date, TimeOnly Start, TimeOnly End, Guid? ShiftId)>();

        foreach (var rule in rules)
        {
            foreach (var work in works)
            {
                if (!IsShiftInScope(rule, work, tagsByShift))
                {
                    continue;
                }

                var slotStart = work.Date.ToDateTime(work.StartTime);
                var slotEnd = work.EndTime <= work.StartTime
                    ? work.Date.AddDays(1).ToDateTime(work.EndTime)
                    : work.Date.ToDateTime(work.EndTime);

                if (!WindowBlocks(
                        rule.SeasonFromMonth, rule.SeasonFromDay, rule.SeasonToMonth, rule.SeasonToDay,
                        ToMinutes(rule.DailyStart), ToMinutes(rule.DailyEnd), slotStart, slotEnd))
                {
                    continue;
                }

                if (!reported.Add((rule.Id, work.Date, work.StartTime, work.EndTime, work.ShiftId)))
                {
                    continue;
                }

                entries.Add(BuildEntry(clientId, clientName, work.Date, rule, mode));
            }
        }

        return entries;
    }

    private async Task<List<CandidateWork>> LoadWorksAsync(
        Guid clientId,
        DateOnly from,
        DateOnly to,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId
                && !w.IsDeleted
                && w.AnalyseToken == analyseToken
                && w.CurrentDate >= from
                && w.CurrentDate <= to)
            .Select(w => new { w.CurrentDate, w.StartTime, w.EndTime, w.ShiftId })
            .ToListAsync(cancellationToken);

        return works
            .Select(w => new CandidateWork(w.CurrentDate, w.StartTime, w.EndTime, NullIfEmpty(w.ShiftId)))
            .ToList();
    }

    // Group-tag scope is resolved by SHIFT ID with NO AnalyseToken filter - the same token-independent
    // treatment the wizard qualification eligibility uses (ShiftRequiredQualification is queried by shift
    // id, no token). Correctness comes from the shift-id scoping, not a token filter: a scenario clones a
    // shift under a FRESH id and clones its GroupItem links to that clone id (AnalyseScenarioService.
    // CloneShifts), so a cloned work references the clone shift id and finds the cloned link, while a real
    // work references the real shift id and finds the real link. The two id spaces are disjoint, so an
    // unfiltered query never double-counts or leaks across worlds. Filtering AnalyseToken == null here
    // would (wrongly) blind every tagged rule in scenario mode, where the work carries the clone shift id.
    // Returns, per shift id, the list of (group name, validity window) links.
    private async Task<Dictionary<Guid, List<GroupTagLink>>> LoadGroupTagsByShiftAsync(
        IReadOnlyList<CandidateWork> works,
        CancellationToken cancellationToken)
    {
        var shiftIds = works
            .Where(w => w.ShiftId.HasValue)
            .Select(w => w.ShiftId!.Value)
            .Distinct()
            .ToList();
        if (shiftIds.Count == 0)
        {
            return new Dictionary<Guid, List<GroupTagLink>>();
        }

        var links = await _context.GroupItem
            .AsNoTracking()
            .Where(gi => gi.ShiftId != null
                && shiftIds.Contains(gi.ShiftId.Value)
                && !gi.IsDeleted)
            .Join(
                _context.Group.AsNoTracking().Where(g => !g.IsDeleted),
                gi => gi.GroupId,
                g => g.Id,
                (gi, g) => new { ShiftId = gi.ShiftId!.Value, g.Name, gi.ValidFrom, gi.ValidUntil })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, List<GroupTagLink>>();
        foreach (var link in links)
        {
            if (!result.TryGetValue(link.ShiftId, out var list))
            {
                list = [];
                result[link.ShiftId] = list;
            }

            list.Add(new GroupTagLink(link.Name, link.ValidFrom, link.ValidUntil));
        }

        return result;
    }

    private static bool IsShiftInScope(
        RestrictedTimeWindowRule rule,
        CandidateWork work,
        IReadOnlyDictionary<Guid, List<GroupTagLink>> tagsByShift)
    {
        if (string.IsNullOrEmpty(rule.AppliesToGroupTag))
        {
            return true;
        }

        if (!work.ShiftId.HasValue || !tagsByShift.TryGetValue(work.ShiftId.Value, out var links))
        {
            return false;
        }

        var date = work.Date.ToDateTime(TimeOnly.MinValue);
        foreach (var link in links)
        {
            if (!string.Equals(link.GroupName, rule.AppliesToGroupTag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if ((link.ValidFrom == null || link.ValidFrom.Value.Date <= date)
                && (link.ValidUntil == null || link.ValidUntil.Value.Date >= date))
            {
                return true;
            }
        }

        return false;
    }

    private static Guid? NullIfEmpty(Guid shiftId) => shiftId == Guid.Empty ? null : shiftId;

    private static int ToMinutes(TimeOnly time) => (int)time.ToTimeSpan().TotalMinutes;

    // Pure season + daily-window overlap test. IDENTICAL to CoreRestrictedTimeWindow.WouldBlock on the
    // optimizer side (the optimizer must not reference Klacks.Api); the shared unit-test vector table
    // drives BOTH copies and asserts they agree, so the duplication cannot silently drift.
    internal static bool WindowBlocks(
        int fromMonth,
        int fromDay,
        int toMonth,
        int toDay,
        int dailyStartMinutes,
        int dailyEndMinutes,
        DateTime slotStart,
        DateTime slotEnd)
    {
        if (slotEnd <= slotStart)
        {
            return false;
        }

        var startDay = DateOnly.FromDateTime(slotStart);
        for (var offset = -1; offset <= 1; offset++)
        {
            var anchor = startDay.AddDays(offset);
            if (!SeasonContains(anchor.Month, anchor.Day, fromMonth, fromDay, toMonth, toDay))
            {
                continue;
            }

            var midnight = anchor.ToDateTime(TimeOnly.MinValue);
            var windowStart = midnight.AddMinutes(dailyStartMinutes);
            var windowEnd = dailyEndMinutes > dailyStartMinutes
                ? midnight.AddMinutes(dailyEndMinutes)
                : midnight.AddMinutes(MinutesPerDay + dailyEndMinutes);

            if (slotStart < windowEnd && windowStart < slotEnd)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool SeasonContains(int month, int day, int fromMonth, int fromDay, int toMonth, int toDay)
    {
        var current = (month * 100) + day;
        var from = (fromMonth * 100) + fromDay;
        var to = (toMonth * 100) + toDay;

        return from <= to
            ? current >= from && current <= to
            : current >= from || current <= to;
    }

    private static ScheduleValidationNotificationDto BuildEntry(
        Guid clientId,
        string clientName,
        DateOnly reportDate,
        RestrictedTimeWindowRule rule,
        RuleEnforcementMode mode)
    {
        var isBlocked = mode == RuleEnforcementMode.Block;
        var commentParams = new Dictionary<string, string>
        {
            ["dailyStart"] = rule.DailyStart.ToString("HH:mm", CultureInfo.InvariantCulture),
            ["dailyEnd"] = rule.DailyEnd.ToString("HH:mm", CultureInfo.InvariantCulture),
            ["seasonFrom"] = FormatMonthDay(rule.SeasonFromMonth, rule.SeasonFromDay),
            ["seasonTo"] = FormatMonthDay(rule.SeasonToMonth, rule.SeasonToDay),
            ["groupTag"] = rule.AppliesToGroupTag,
        };
        if (isBlocked)
        {
            commentParams[ComplianceRuleNames.EnforcementRuleParamKey] = ComplianceRuleNames.RestrictedTimeWindow;
        }

        return new ScheduleValidationNotificationDto
        {
            Type = isBlocked ? ScheduleValidationType.Error : ScheduleValidationType.Warning,
            ClientId = clientId,
            ClientName = clientName,
            Date = reportDate,
            Comment = ScheduleValidationKeys.RestrictedTimeWindow,
            CommentParams = commentParams,
        };
    }

    private static string FormatMonthDay(int month, int day) =>
        string.Create(CultureInfo.InvariantCulture, $"{month:00}-{day:00}");

    private readonly record struct GroupTagLink(string GroupName, DateTime? ValidFrom, DateTime? ValidUntil);
}
