// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IProactiveActionReporter"/>. Stash-first, exactly as ScheduledTaskRunner delivers
/// a cron result: the durable note is written BEFORE any live delivery is attempted, and is only marked
/// delivered once the live push has actually gone out. Stashing only in the offline branch loses the
/// report whenever the presence check is a false positive or the push fails - and this report is the
/// only trace a planner gets that Klacksy changed something on its own.
/// </summary>
/// <param name="agentRepository">Resolves the default agent a pending note has to belong to.</param>
/// <param name="pendingNotes">Durable stash read back on the recipient's next conversation.</param>
/// <param name="notification">Live push for a recipient who is connected right now.</param>
/// <param name="logger">Records a report that could not be stashed - the one failure that loses it.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ProactiveActionReporter : IProactiveActionReporter
{
    private const string NoDefaultAgentMessage =
        "No default agent exists, so the mandatory report for a proactive action could not be stashed; "
        + "the action itself has already happened";

    private const string StashFailedMessage =
        "The mandatory report for a proactive action could not be stashed for user {UserId}; "
        + "the action itself has already happened";

    private const string AcknowledgeFailedMessage =
        "Proactive action report {NoteId} was pushed live but could not be marked delivered";

    private readonly IAgentRepository _agentRepository;
    private readonly IPendingUserNoteRepository _pendingNotes;
    private readonly IAssistantNotificationService _notification;
    private readonly ILogger<ProactiveActionReporter> _logger;

    public ProactiveActionReporter(
        IAgentRepository agentRepository,
        IPendingUserNoteRepository pendingNotes,
        IAssistantNotificationService notification,
        ILogger<ProactiveActionReporter> logger)
    {
        _agentRepository = agentRepository;
        _pendingNotes = pendingNotes;
        _notification = notification;
        _logger = logger;
    }

    public async Task<bool> ReportAsync(
        Guid recipientUserId, string message, CancellationToken cancellationToken = default)
    {
        var note = await StashAsync(recipientUserId, message, cancellationToken);
        if (note is null)
        {
            return false;
        }

        await PushLiveAsync(note, recipientUserId, message, cancellationToken);
        return true;
    }

    private async Task<PendingUserNote?> StashAsync(
        Guid recipientUserId, string message, CancellationToken cancellationToken)
    {
        try
        {
            var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
            if (agent is null)
            {
                _logger.LogWarning(NoDefaultAgentMessage);
                return null;
            }

            var note = new PendingUserNote
            {
                Id = Guid.NewGuid(),
                AgentId = agent.Id,
                UserId = recipientUserId,
                Content = message,
                Topic = AgentConditionActionDefaults.ReportNoteTopic
            };

            await _pendingNotes.AddAsync(note, cancellationToken);
            return note;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, StashFailedMessage, recipientUserId);
            return null;
        }
    }

    private async Task PushLiveAsync(
        PendingUserNote note, Guid recipientUserId, string message, CancellationToken cancellationToken)
    {
        var userId = recipientUserId.ToString();

        try
        {
            if (!await _notification.IsUserConnectedAsync(userId))
            {
                return;
            }

            await _notification.SendProactiveMessageAsync(userId, message);
            await _pendingNotes.MarkDeliveredAsync(note.AgentId, recipientUserId, new[] { note.Id }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, AcknowledgeFailedMessage, note.Id);
        }
    }
}
