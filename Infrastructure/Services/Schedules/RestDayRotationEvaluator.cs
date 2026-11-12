// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IRestDayRotationEvaluator"/> (K10). For every active rule and every candidate
/// date, the trailing window ending on the most recent occurrence of the rule's weekday is inspected:
/// it spans exactly WindowWeeks occurrences of that weekday, and each occurrence counts as work-free
/// when no Work row touches it. Runs as a direct database check over the full window (the
/// PeriodCapEvaluator pattern) because the window (typically 4-26 weeks) far exceeds the +-2-week
/// timeline the ScheduleValidationBuilder callers load. Warning escalates to Error when the
/// restDayRotation compliance rule's enforcement mode is Block. The evaluation of the planned rows of several
/// clients runs the same per-client inspection; it reads the rule list and the mode once, the contract data once
/// per distinct day, the employment starts in one read and the Work rows once per distinct load range for all
/// clients that inspect it; a value that was not read ahead throws instead of being read for the client alone.
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
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
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

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedRowsAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default)
    {
        if (plannedRows.Count == 0)
        {
            return [];
        }

        var allRules = await _ruleRepository.GetAllActiveAsync();
        if (allRules.Count == 0)
        {
            return [];
        }

        var clients = plannedRows
            .GroupBy(row => row.ClientId)
            .Select(group => new PlannedClient(
                group.Key,
                group.Select(row => row.Date).Distinct().ToList(),
                group.Select(row => ToInterval(row.Date, row.StartTime, row.EndTime)).ToList()))
            .ToList();

        var contracts = await ClientContractDataBatch.LoadAsync(
            _contractDataProvider, clients.Select(client => (client.ClientId, client.CandidateDates.Max())));
        var evaluations = clients
            .Select(client => (Client: client, Contract: contracts[(client.ClientId, client.CandidateDates.Max())]))
            .Select(evaluation => (
                evaluation.Client,
                Rules: ApplicableRules(allRules, evaluation.Contract.SchedulingRuleId),
                evaluation.Contract.MinRestDays))
            .Where(evaluation => evaluation.Rules.Count > 0)
            .ToList();
        if (evaluations.Count == 0)
        {
            return [];
        }

        var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.RestDayRotation);
        var membershipStarts = await _membershipStartResolver.GetValidFromForClientsAsync(
            evaluations.Select(evaluation => evaluation.Client.ClientId).ToList(), cancellationToken);
        var worksByRange = await LoadWorksOfClientsAsync(
            evaluations.SelectMany(evaluation => InspectedWindows(
                    evaluation.Rules, evaluation.Client.CandidateDates, membershipStarts.GetValueOrDefault(evaluation.Client.ClientId))
                .Select(window => (
                    evaluation.Client.ClientId,
                    window.WindowStart.AddDays(-1),
                    window.WindowEnd.AddDays(LookAheadDays(evaluation.MinRestDays))))),
            analyseToken,
            cancellationToken);

        var entries = new List<ScheduleValidationNotificationDto>();
        foreach (var (client, rules, minimumRestDays) in evaluations)
        {
            entries.AddRange(await BuildEntriesAsync(
                client.ClientId,
                string.Empty,
                client.CandidateDates,
                client.PlannedWorks,
                rules,
                minimumRestDays,
                membershipStarts[client.ClientId],
                mode,
                (loadStart, loadEnd) => Task.FromResult(worksByRange[(client.ClientId, loadStart, loadEnd)])));
        }

        return entries;
    }

    private sealed record PlannedClient(
        Guid ClientId,
        IReadOnlyList<DateOnly> CandidateDates,
        IReadOnlyList<WorkInterval> PlannedWorks);

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
        var rules = ApplicableRules(allRules, effectiveData.SchedulingRuleId);
        if (rules.Count == 0)
        {
            return [];
        }

        var mode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.RestDayRotation);
        var membershipStart = await _membershipStartResolver.GetValidFromAsync(clientId);

        return await BuildEntriesAsync(
            clientId,
            clientName,
            candidateDates,
            plannedWorks,
            rules,
            effectiveData.MinRestDays,
            membershipStart,
            mode,
            (loadStart, loadEnd) => LoadWorksAsync(clientId, loadStart, loadEnd, analyseToken, cancellationToken));
    }

    private static List<RestDayRotationRule> ApplicableRules(List<RestDayRotationRule> rules, Guid? schedulingRuleId) =>
        rules
            .Where(r => r.SchedulingRuleId == null || r.SchedulingRuleId == schedulingRuleId)
            .ToList();

    private static int LookAheadDays(decimal minimumRestDays) =>
        (int)Math.Ceiling(CalendarWeekRestDays.MinimumFreeBlock(minimumRestDays).TotalDays) + LookAheadPaddingDays;

    // A Work anchored on a candidate date can occupy BOTH that date and the following day
    // (cross-midnight), so the window anchored at the next day must be inspected as well - otherwise
    // a Saturday 22:00-07:00 placement would only ever check the window ending on the PREVIOUS
    // Sunday and the Sunday it actually occupies would never be an inspected occurrence.
    private static IEnumerable<(RestDayRotationRule Rule, DateOnly WindowStart, DateOnly WindowEnd)> InspectedWindows(
        IReadOnlyList<RestDayRotationRule> rules,
        IReadOnlyList<DateOnly> candidateDates,
        DateOnly? membershipStart)
    {
        var reportedWindows = new HashSet<(Guid RuleId, DateOnly WindowEnd)>();
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

                yield return (rule, windowStart, windowEnd);
            }
        }
    }

    private static async Task<List<ScheduleValidationNotificationDto>> BuildEntriesAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<DateOnly> candidateDates,
        IReadOnlyList<WorkInterval> plannedWorks,
        IReadOnlyList<RestDayRotationRule> rules,
        decimal minimumRestDays,
        DateOnly? membershipStart,
        RuleEnforcementMode mode,
        Func<DateOnly, DateOnly, Task<List<WorkInterval>>> worksOfRange)
    {
        var minimumFreeBlock = CalendarWeekRestDays.MinimumFreeBlock(minimumRestDays);
        var lookAheadDays = LookAheadDays(minimumRestDays);
        var entries = new List<ScheduleValidationNotificationDto>();

        foreach (var (rule, windowStart, windowEnd) in InspectedWindows(rules, candidateDates, membershipStart))
        {
            var persistedWorks = await worksOfRange(windowStart.AddDays(-1), windowEnd.AddDays(lookAheadDays));
            var works = persistedWorks.Concat(plannedWorks).ToList();

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

        return entries;
    }

    /// <summary>
    /// Reads the Work rows of the requested (client, range) pairs with one query per distinct range and chunk of
    /// IdListQueryLimits.ChunkSize clients, through the query of the single-client read. Every requested pair gets an entry.
    /// </summary>
    /// <param name="requests">The (client, first day, last day) triples to read</param>
    /// <param name="analyseToken">Scenario token; null reads the real schedule</param>
    private async Task<Dictionary<(Guid ClientId, DateOnly Start, DateOnly End), List<WorkInterval>>> LoadWorksOfClientsAsync(
        IEnumerable<(Guid ClientId, DateOnly Start, DateOnly End)> requests,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var result = requests.Distinct().ToDictionary(request => request, _ => new List<WorkInterval>());

        foreach (var range in result.Keys.GroupBy(request => (request.Start, request.End), request => request.ClientId).ToList())
        {
            foreach (var chunk in range.Chunk(IdListQueryLimits.ChunkSize))
            {
                var rows = await QueryWorksOfClients(chunk, range.Key.Start, range.Key.End, analyseToken)
                    .ToListAsync(cancellationToken);
                foreach (var row in rows)
                {
                    result[(row.ClientId, range.Key.Start, range.Key.End)]
                        .Add(ToInterval(row.Date, row.StartTime, row.EndTime));
                }
            }
        }

        return result;
    }

    internal IQueryable<ClientWorkRow> QueryWorksOfClients(
        Guid[] clientIds, DateOnly loadStart, DateOnly loadEnd, Guid? analyseToken)
        => _context.Work
            .AsNoTracking()
            .Where(w => clientIds.Contains(w.ClientId)
                && !w.IsDeleted
                && w.AnalyseToken == analyseToken
                && w.CurrentDate >= loadStart
                && w.CurrentDate <= loadEnd)
            .Select(w => new ClientWorkRow(w.ClientId, w.CurrentDate, w.StartTime, w.EndTime));

    private async Task<List<WorkInterval>> LoadWorksAsync(
        Guid clientId,
        DateOnly loadStart,
        DateOnly loadEnd,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var works = await QueryWorksOfClients([clientId], loadStart, loadEnd, analyseToken)
            .ToListAsync(cancellationToken);

        return works.Select(work => ToInterval(work.Date, work.StartTime, work.EndTime)).ToList();
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
