// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cancels (soft-deletes) one of the current user's recurring tasks, resolved by id or by name. Only
/// the owner's own tasks can be cancelled.
/// </summary>
/// <param name="taskId">The id of the task to cancel (preferred when known).</param>
/// <param name="name">The name of the task to cancel (used when taskId is not given).</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("cancel_recurring_task")]
public class CancelRecurringTaskSkill : BaseSkillImplementation
{
    private readonly IScheduledTaskRepository _repository;

    public CancelRecurringTaskSkill(IScheduledTaskRepository repository)
    {
        _repository = repository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var taskId = GetParameter<Guid?>(parameters, "taskId");
        var name = GetParameter<string>(parameters, "name")?.Trim();

        ScheduledTask? task = null;
        if (taskId is { } id && id != Guid.Empty)
        {
            task = await _repository.GetByIdAsync(id, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(name))
        {
            task = await _repository.GetByOwnerAndNameAsync(context.UserId, name, cancellationToken);
        }
        else
        {
            return SkillResult.Error("Provide either taskId or name to cancel a scheduled task.");
        }

        if (task is null || task.OwnerUserId != context.UserId)
        {
            return SkillResult.Error("No matching scheduled task was found for you.");
        }

        await _repository.DeleteAsync(task.Id, cancellationToken);

        return SkillResult.SuccessResult(new { id = task.Id, name = task.Name }, $"Cancelled scheduled task '{task.Name}'.");
    }
}
