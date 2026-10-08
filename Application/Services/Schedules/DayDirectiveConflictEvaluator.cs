// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IDayDirectiveConflictEvaluator"/>. Loads the schedule commands of the planned clients and days
/// (of the run's scenario; null = real plan), combines them per day with <see cref="ScheduleCommandKeywordCombiner"/>
/// (the rule Wizard 1, Wizard 2 and the recovery snapshot apply) and reports a planned row whose shift kind
/// (<see cref="ShiftTypeInference.FromSpan"/>, keyed on the start date) the day does not allow. The conflict is an Error
/// without the enforcement tag: find_replacement excludes the candidate, place_work and the plan partition refuse the
/// placement, while the direct write paths treat it like a collision (persisted, surfaced by the post-commit check),
/// because a planner may knowingly plan against a wish.
/// </summary>
/// <param name="scheduleCommandRepository">Reads the schedule commands of the planned clients and days</param>
/// <param name="keywordProvider">Admin-configured directive tokens</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class DayDirectiveConflictEvaluator : IDayDirectiveConflictEvaluator
{
    public const string DirectiveParamKey = "directive";

    private readonly IScheduleCommandRepository _scheduleCommandRepository;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;

    public DayDirectiveConflictEvaluator(
        IScheduleCommandRepository scheduleCommandRepository,
        IScheduleCommandKeywordProvider keywordProvider)
    {
        _scheduleCommandRepository = scheduleCommandRepository;
        _keywordProvider = keywordProvider;
    }

    public async Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        Guid? analyseToken,
        CancellationToken cancellationToken = default)
    {
        if (plannedRows.Count == 0)
        {
            return [];
        }

        var clientIds = plannedRows.Select(r => r.ClientId).Distinct().ToList();
        var commands = await _scheduleCommandRepository.GetByClientsAndDateRangeAsync(
            clientIds, plannedRows.Min(r => r.Date), plannedRows.Max(r => r.Date), analyseToken, cancellationToken);
        if (commands.Count == 0)
        {
            return [];
        }

        var keywordMap = ScheduleCommandKeywordMapper.BuildMap(await _keywordProvider.GetAsync(cancellationToken));
        var directiveByDay = ScheduleCommandKeywordCombiner.CombinePerDay(commands, keywordMap);

        var conflicts = new List<ScheduleValidationNotificationDto>();
        foreach (var row in plannedRows)
        {
            if (!directiveByDay.TryGetValue((row.ClientId, row.Date), out var directive)
                || ScheduleCommandKeywordCombiner.Allows(directive, ShiftTypeInference.FromSpan(row.StartTime, row.EndTime)))
            {
                continue;
            }

            conflicts.Add(new ScheduleValidationNotificationDto
            {
                Type = ScheduleValidationType.Error,
                ClientId = row.ClientId,
                ClientName = string.Empty,
                Date = row.Date,
                Comment = ScheduleValidationKeys.DayDirective,
                CommentParams = new Dictionary<string, string> { [DirectiveParamKey] = directive.ToString() },
            });
        }

        return conflicts;
    }
}
