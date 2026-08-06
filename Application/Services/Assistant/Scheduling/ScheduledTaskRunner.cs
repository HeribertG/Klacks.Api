// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes due scheduled tasks deterministically. Per tick it reads the due tasks, decides per task
/// (fire / skip-stale) via <see cref="ScheduledTaskDuePolicy"/>, atomically claims the occurrence so it
/// cannot double-fire, runs the resolved action — a static reminder, or a single skill under a token
/// minted for the owner at fire time with the autonomy gate bypassed (consent was given when the
/// schedule was created) — delivers the result to the owner (live proactive message, or a durable
/// pending note when offline) and records the outcome. No LLM and no further user input are involved at
/// fire time. Rights come from the owner's CURRENT roles, not from a set frozen at authoring time, so a
/// revoked role takes effect on the next run. Because nobody is there to confirm anything, skill actions
/// pass <see cref="IUnattendedSkillPolicy"/> first; a refusal there disables the task instead of
/// retrying it every tick, while a refusal to mint a token leaves it enabled — that condition is
/// usually temporary.
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
    private readonly IUnattendedSkillPolicy _unattendedPolicy;
    private readonly IInternalTokenIssuer _internalTokenIssuer;
    private readonly ILogger<ScheduledTaskRunner> _logger;
    private readonly ScheduledTaskDuePolicy _policy = new();

    public ScheduledTaskRunner(
        IScheduledTaskRepository repository,
        ISkillExecutor skillExecutor,
        IAssistantNotificationService notification,
        IPendingUserNoteRepository pendingNotes,
        IAgentRepository agentRepository,
        IUnattendedSkillPolicy unattendedPolicy,
        IInternalTokenIssuer internalTokenIssuer,
        ILogger<ScheduledTaskRunner> logger)
    {
        _repository = repository;
        _skillExecutor = skillExecutor;
        _notification = notification;
        _pendingNotes = pendingNotes;
        _agentRepository = agentRepository;
        _unattendedPolicy = unattendedPolicy;
        _internalTokenIssuer = internalTokenIssuer;
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
                disable: false,
                cancellationToken);
            return;
        }

        var (status, body, disable) = await ExecuteActionAsync(task, cancellationToken);
        await DeliverAsync(task, body, cancellationToken);
        await RecordOutcomeAsync(task, now, status, body, newNext, incrementRun: true, disable, cancellationToken);
    }

    private async Task<(string Status, string Body, bool Disable)> ExecuteActionAsync(ScheduledTask task, CancellationToken cancellationToken)
    {
        if (string.Equals(task.ActionType, ScheduledTaskActionTypes.Reminder, StringComparison.OrdinalIgnoreCase))
        {
            return (ScheduledTaskRunStatus.Ok, task.MessageText ?? string.Empty, false);
        }

        if (string.IsNullOrWhiteSpace(task.SkillName))
        {
            return (ScheduledTaskRunStatus.Error, "No skill configured for this task.", false);
        }

        // The run acts under a freshly minted token carrying the owner's CURRENT roles, not the
        // permission set frozen when the schedule was created — revoking a role now takes effect on the
        // next run. A refusal does NOT disable the task: a locked-out account gets unlocked and a
        // missing role gets granted, so the task must survive to run again once that happens.
        var token = await _internalTokenIssuer.IssueForOwnerAsync(task.OwnerUserId, cancellationToken: cancellationToken);
        if (!token.Success)
        {
            _logger.LogWarning(
                "Scheduled task {TaskId} could not run skill {SkillName}: {Reason}",
                task.Id, task.SkillName, token.Reason);
            return (ScheduledTaskRunStatus.Error, token.Reason!, false);
        }

        var ownerPermissions = Permissions.ExpandRoles(token.Roles);
        var decision = _unattendedPolicy.Decide(task.SkillName, ownerPermissions);
        if (!decision.Allowed)
        {
            _logger.LogWarning(
                "Scheduled task {TaskId} refused and disabled before running skill {SkillName}: {Reason}",
                task.Id, task.SkillName, decision.Reason);
            return (ScheduledTaskRunStatus.Error, decision.Reason!, true);
        }

        var context = new SkillExecutionContext
        {
            UserId = task.OwnerUserId,
            TenantId = Guid.Empty,
            UserName = task.OwnerUserName,
            UserPermissions = ownerPermissions,
            AccessToken = token.Token,
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
            ? (ScheduledTaskRunStatus.Ok, message, false)
            : (ScheduledTaskRunStatus.Error, message, false);
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
        bool disable,
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

        if (disable || (task.MaxRuns is { } max && task.RunCount >= max))
        {
            task.IsEnabled = false;
            task.NextRunUtc = null;
        }

        await _repository.UpdateAsync(task, cancellationToken);
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
