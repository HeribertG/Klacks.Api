// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the current user's recurring (cron) tasks with their schedule, next run and last outcome.
/// Read-only.
/// </summary>

using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_recurring_tasks")]
public class ListRecurringTasksSkill : BaseSkillImplementation
{
    private readonly IScheduledTaskRepository _repository;

    public ListRecurringTasksSkill(IScheduledTaskRepository repository)
    {
        _repository = repository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _repository.GetByOwnerAsync(context.UserId, includeDisabled: true, cancellationToken);

        var items = tasks
            .OrderByDescending(t => t.IsEnabled)
            .ThenBy(t => t.NextRunUtc ?? DateTime.MaxValue)
            .Select(t => new
            {
                id = t.Id,
                name = t.Name,
                cronExpression = t.CronExpression,
                timeZone = t.TimeZoneId,
                actionType = t.ActionType,
                skillName = t.SkillName,
                enabled = t.IsEnabled,
                nextRun = t.NextRunUtc is { } next ? CronSchedule.FormatLocal(next, t.TimeZoneId) : null,
                lastStatus = t.LastStatus,
                lastRunUtc = t.LastRunUtc,
                runCount = t.RunCount,
                maxRuns = t.MaxRuns
            })
            .ToList();

        var message = items.Count == 0
            ? "You have no scheduled tasks."
            : $"You have {items.Count} scheduled task(s).";

        return SkillResult.SuccessResult(items, message);
    }
}
