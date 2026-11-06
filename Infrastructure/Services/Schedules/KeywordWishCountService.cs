// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IKeywordWishCountService"/>. Loads the recognized schedule commands, the top-level works and the
/// absences (breaks without a parent work) of the plan, keys every work on its start day and shift kind
/// (<see cref="ShiftTypeInference.FromSpan"/>, as the day-directive check does) and hands them to
/// <see cref="KeywordWishCalculator"/>. A positive wish on an absence day is dropped, no plan can fulfil it. The same
/// keyword days answer whether a range has any restricting command (the AutoWizard's soft-retry gate).
/// </summary>
/// <param name="context">EF Core database context (works and breaks of the plan)</param>
/// <param name="scheduleCommandRepository">Reads the schedule commands of the employees and days</param>
/// <param name="keywordProvider">Admin-configured directive tokens</param>

using Klacks.Api.Application.DTOs.Schedules.KeywordWishes;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Constraints.Keywords;
using Klacks.ScheduleOptimizer.Models;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class KeywordWishCountService : IKeywordWishCountService
{
    private readonly DataBaseContext _context;
    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;

    public KeywordWishCountService(
        DataBaseContext context,
        IScheduleCommandRepository scheduleCommandRepository,
        IScheduleCommandKeywordProvider keywordProvider)
    {
        _context = context;
        _scheduleCommandRepository = scheduleCommandRepository;
        _keywordProvider = keywordProvider;
    }

    public async Task<KeywordWishesDto> CountAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0 || from > until)
        {
            return KeywordWishesDto.Empty;
        }

        var ids = clientIds.Distinct().ToList();
        var days = await LoadKeywordDaysAsync(ids, from, until, analyseToken, cancellationToken);
        if (days.Count == 0)
        {
            return KeywordWishesDto.Empty;
        }

        await DropPositiveWishOnAbsenceDaysAsync(days, ids, from, until, analyseToken, cancellationToken);
        var worked = await LoadWorkedKindsAsync(ids, from, until, analyseToken, cancellationToken);
        return ToDto(KeywordWishCalculator.Evaluate(days, worked, collectItems: true));
    }

    public async Task<bool> HasRestrictingWishesAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0 || from > until)
        {
            return false;
        }

        var days = await LoadKeywordDaysAsync(clientIds.Distinct().ToList(), from, until, analyseToken, cancellationToken);
        return days.Values.Any(day => day.RestrictionOutcome is not null);
    }

    private async Task<Dictionary<(string AgentId, DateOnly Date), KeywordDay>> LoadKeywordDaysAsync(
        List<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var commands = await _scheduleCommandRepository.GetByClientsAndDateRangeAsync(clientIds, from, until, analyseToken, cancellationToken);
        if (commands.Count == 0)
        {
            return [];
        }

        var keywordMap = ScheduleCommandKeywordMapper.BuildMap(await _keywordProvider.GetAsync(cancellationToken));
        var recognized = new List<(string AgentId, DateOnly Date, ScheduleCommandKeyword Keyword)>(commands.Count);
        foreach (var command in commands)
        {
            if (ScheduleCommandKeywordMapper.TryMap(command.CommandKeyword, keywordMap, out var keyword))
            {
                recognized.Add((command.ClientId.ToString(), command.CurrentDate, keyword));
            }
        }

        return KeywordWishCalculator.BuildDays(recognized);
    }

    private async Task DropPositiveWishOnAbsenceDaysAsync(
        Dictionary<(string AgentId, DateOnly Date), KeywordDay> days,
        List<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var absences = await _context.Break
            .AsNoTracking()
            .Where(b => clientIds.Contains(b.ClientId)
                && b.ParentWorkId == null
                && b.CurrentDate >= from
                && b.CurrentDate <= until
                && (b.AnalyseToken == analyseToken || (b.AnalyseToken == null && analyseToken == null)))
            .Select(b => new { b.ClientId, b.CurrentDate })
            .ToListAsync(cancellationToken);

        foreach (var absence in absences)
        {
            var key = (absence.ClientId.ToString(), absence.CurrentDate);
            if (days.TryGetValue(key, out var day))
            {
                days[key] = day.WithoutPositiveWish();
            }
        }
    }

    private async Task<Dictionary<(string AgentId, DateOnly Date), int>> LoadWorkedKindsAsync(
        List<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => clientIds.Contains(w.ClientId)
                && w.ParentWorkId == null
                && w.CurrentDate >= from
                && w.CurrentDate <= until
                && (w.AnalyseToken == analyseToken || (w.AnalyseToken == null && analyseToken == null)))
            .Select(w => new { w.ClientId, w.CurrentDate, w.StartTime, w.EndTime })
            .ToListAsync(cancellationToken);

        var worked = new Dictionary<(string AgentId, DateOnly Date), int>();
        foreach (var work in works)
        {
            var key = (work.ClientId.ToString(), work.CurrentDate);
            worked[key] = worked.GetValueOrDefault(key)
                | KeywordShiftKinds.FromShiftTypeIndex(ShiftTypeInference.FromSpan(work.StartTime, work.EndTime));
        }

        return worked;
    }

    private static KeywordWishesDto ToDto(KeywordWishResult result)
        => new(
            result.Tally.BrokenFree,
            result.Tally.BrokenOnly,
            result.Tally.BrokenNo,
            result.Tally.Unfulfilled,
            result.Items
                .Select(i => new KeywordWishItemDto(Guid.Parse(i.AgentId), i.Date, i.Keyword.ToString(), i.Outcome.ToString()))
                .ToList());
}
