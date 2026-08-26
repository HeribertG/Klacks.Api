// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the current user's recurring (cron) tasks with their schedule, next run and last outcome.
/// Read-only. A task refused by the unattended policy for a cause the owner can fix is PAUSED rather
/// than disabled - it keeps IsEnabled true - so the paused state and its reason are reported as their
/// own fields and counted in the summary line. Reporting only "enabled" would show a task the owner was
/// just told is paused as if it were still running.
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
            .ThenBy(t => t.IsPaused)
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
                paused = t.IsPaused,
                pausedReason = t.PausedReason,
                allowIrreversibleUnattended = t.AllowIrreversibleUnattended,
                nextRun = t.NextRunUtc is { } next ? CronSchedule.FormatLocal(next, t.TimeZoneId) : null,
                lastStatus = t.LastStatus,
                lastRunUtc = t.LastRunUtc,
                runCount = t.RunCount,
                maxRuns = t.MaxRuns
            })
            .ToList();

        var pausedCount = tasks.Count(t => t.IsPaused);
        var message = items.Count == 0
            ? "You have no scheduled tasks."
            : pausedCount == 0
                ? $"You have {items.Count} scheduled task(s)."
                : $"You have {items.Count} scheduled task(s), {pausedCount} of them paused and not running " +
                  "until the cause is fixed and the task is scheduled again under the same name.";

        return SkillResult.SuccessResult(items, message);
    }
}
