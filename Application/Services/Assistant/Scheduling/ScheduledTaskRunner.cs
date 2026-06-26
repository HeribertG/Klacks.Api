// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes due scheduled tasks deterministically. Per tick it reads the due tasks, decides per task
/// (fire / skip-stale) via <see cref="ScheduledTaskDuePolicy"/>, atomically claims the occurrence so it
/// cannot double-fire, runs the resolved action — a static reminder or a single skill under the owner's
/// captured identity with the autonomy gate bypassed (consent was given when the schedule was created)
/// — delivers the result to the owner (live proactive message, or a durable pending note when offline)
/// and records the outcome. No LLM and no further user input are involved at fire time.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant.Scheduling;

public sealed class ScheduledTaskRunner : IScheduledTaskRunner
{
    private static readonly TimeSpan CatchUpWindow = TimeSpan.FromMinutes(15);
    private const int MaxResultLength = 500;

    private readonly IScheduledTaskRepository _repository;
    private readonly ISkillExecutor _skillExecutor;
    private readonly IAssistantNotificationService _notification;
    private readonly IPendingUserNoteRepository _pendingNotes;
    private readonly IAgentRepository _agentRepository;
    private readonly ILogger<ScheduledTaskRunner> _logger;
    private readonly ScheduledTaskDuePolicy _policy = new();

    public ScheduledTaskRunner(
        IScheduledTaskRepository repository,
        ISkillExecutor skillExecutor,
        IAssistantNotificationService notification,
        IPendingUserNoteRepository pendingNotes,
        IAgentRepository agentRepository,
        ILogger<ScheduledTaskRunner> logger)
    {
        _repository = repository;
        _skillExecutor = skillExecutor;
        _notification = notification;
        _pendingNotes = pendingNotes;
        _agentRepository = agentRepository;
        _logger = logger;
    }

    public async Task RunDueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var due = await _repository.GetDueAsync(now, cancellationToken);
        if (due.Count == 0)
        {
            return;
        }

        foreach (var task in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ProcessOneAsync(task, now, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled task {TaskId} failed", task.Id);
            }
        }
    }

    private async Task ProcessOneAsync(ScheduledTask task, DateTime now, CancellationToken cancellationToken)
    {
        var decision = _policy.Decide(task.NextRunUtc, now, CatchUpWindow);
        if (decision == ScheduledTaskRunDecision.NotDue)
        {
            return;
        }

        var newNext = CronSchedule.GetNextOccurrenceUtc(task.CronExpression, task.TimeZoneId, now);

        var claimed = await _repository.TryClaimAsync(task.Id, task.NextRunUtc, newNext, cancellationToken);
        if (!claimed)
        {
            return;
        }

        if (decision == ScheduledTaskRunDecision.SkipStale)
        {
            _logger.LogInformation("Scheduled task {TaskId} occurrence skipped as stale; advanced to {Next}", task.Id, newNext);
            await RecordOutcomeAsync(
                task,
                now,
                ScheduledTaskRunStatus.Skipped,
                "Missed while the server was offline at the scheduled time; advanced to the next run.",
                newNext,
                incrementRun: false,
                cancellationToken);
            return;
        }

        var (status, body) = await ExecuteActionAsync(task, cancellationToken);
        await DeliverAsync(task, body, cancellationToken);
        await RecordOutcomeAsync(task, now, status, body, newNext, incrementRun: true, cancellationToken);
    }

    private async Task<(string Status, string Body)> ExecuteActionAsync(ScheduledTask task, CancellationToken cancellationToken)
    {
        if (string.Equals(task.ActionType, ScheduledTaskActionTypes.Reminder, StringComparison.OrdinalIgnoreCase))
        {
            return (ScheduledTaskRunStatus.Ok, task.MessageText ?? string.Empty);
        }

        if (string.IsNullOrWhiteSpace(task.SkillName))
        {
            return (ScheduledTaskRunStatus.Error, "No skill configured for this task.");
        }

        var context = new SkillExecutionContext
        {
            UserId = task.OwnerUserId,
            TenantId = Guid.Empty,
            UserName = task.OwnerUserName,
            UserPermissions = ParsePermissions(task.OwnerPermissionsCsv),
            UserTimezone = task.TimeZoneId,
            SessionId = $"scheduled-task:{task.Id}",
            BypassAutonomyGate = true
        };

        var invocation = new SkillInvocation
        {
            SkillName = task.SkillName,
            Parameters = ParseParameters(task.ParametersJson)
        };

        var result = await _skillExecutor.ExecuteAsync(invocation, context, cancellationToken);
        var message = string.IsNullOrWhiteSpace(result.Message) ? "Done." : result.Message!;
        return result.Success
            ? (ScheduledTaskRunStatus.Ok, message)
            : (ScheduledTaskRunStatus.Error, message);
    }

    private async Task DeliverAsync(ScheduledTask task, string body, CancellationToken cancellationToken)
    {
        var message = $"⏰ **{task.Name}**\n\n{body}".Trim();
        var userId = task.OwnerUserId.ToString();

        if (_notification.IsUserConnected(userId))
        {
            await _notification.SendProactiveMessageAsync(userId, message);
            return;
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent is null)
        {
            _logger.LogWarning("No default agent; cannot stash pending note for scheduled task {TaskId}", task.Id);
            return;
        }

        await _pendingNotes.AddAsync(
            new PendingUserNote
            {
                AgentId = agent.Id,
                UserId = task.OwnerUserId,
                Content = message,
                Topic = "scheduled-task"
            },
            cancellationToken);
    }

    private async Task RecordOutcomeAsync(
        ScheduledTask task,
        DateTime now,
        string status,
        string resultText,
        DateTime? newNext,
        bool incrementRun,
        CancellationToken cancellationToken)
    {
        task.LastRunUtc = now;
        task.LastStatus = status;
        task.LastResult = Truncate(resultText);
        task.NextRunUtc = newNext;

        if (incrementRun)
        {
            task.RunCount += 1;
        }

        if (task.MaxRuns is { } max && task.RunCount >= max)
        {
            task.IsEnabled = false;
            task.NextRunUtc = null;
        }

        await _repository.UpdateAsync(task, cancellationToken);
    }

    private static IReadOnlyList<string> ParsePermissions(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return Array.Empty<string>();
        }

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static Dictionary<string, object> ParseParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, object>();
        }
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxResultLength ? value : value[..MaxResultLength];
    }
}
