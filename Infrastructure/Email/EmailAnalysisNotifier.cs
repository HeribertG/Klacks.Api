// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Delivers an email analysis summary to every planner and admin. The summary is always stashed as a
/// durable PendingUserNote first; a connected user additionally gets it live as a proactive chat
/// message, and the note is then marked delivered so it is never relayed twice. An offline user — or
/// one whose live send fails — keeps the note, which surfaces on their next chat turn. Delivery
/// failures are logged per user and never abort the batch.
/// </summary>

using System.Text;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Infrastructure.Email;

public class EmailAnalysisNotifier : IEmailAnalysisNotifier
{
    private const string NoteTopic = "email-analysis";

    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly IAssistantNotificationService _notificationService;
    private readonly IPendingUserNoteRepository _pendingNotes;
    private readonly IAgentRepository _agentRepository;
    private readonly ILogger<EmailAnalysisNotifier> _logger;

    public EmailAnalysisNotifier(
        IPlanningAudienceResolver audienceResolver,
        IAssistantNotificationService notificationService,
        IPendingUserNoteRepository pendingNotes,
        IAgentRepository agentRepository,
        ILogger<EmailAnalysisNotifier> logger)
    {
        _audienceResolver = audienceResolver;
        _notificationService = notificationService;
        _pendingNotes = pendingNotes;
        _agentRepository = agentRepository;
        _logger = logger;
    }

    public async Task NotifyAsync(
        ReceivedEmail email,
        EmailAnalysis analysis,
        EmailActionOutcome? actionOutcome = null,
        string? periodLoadSummary = null,
        CancellationToken cancellationToken = default)
    {
        var planners = await _audienceResolver.GetPlanningUserIdsAsync(cancellationToken);
        var admins = await _audienceResolver.GetAdminUserIdsAsync(cancellationToken);
        var recipients = planners.Union(admins, StringComparer.OrdinalIgnoreCase).ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        var message = BuildMessage(email, analysis, actionOutcome, periodLoadSummary);

        foreach (var userId in recipients)
        {
            try
            {
                var note = await StashPendingNoteAsync(userId, message, cancellationToken);

                if (!await _notificationService.IsUserConnectedAsync(userId))
                {
                    continue;
                }

                await _notificationService.SendProactiveMessageAsync(userId, message);
                await AcknowledgeStashedNoteAsync(note, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email analysis notification failed for user {UserId}", userId);
            }
        }
    }

    private async Task<PendingUserNote?> StashPendingNoteAsync(string userId, string message, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return null;
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent is null)
        {
            _logger.LogWarning("No default agent; cannot stash email analysis note for user {UserId}", userId);
            return null;
        }

        var note = new PendingUserNote
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            UserId = userGuid,
            Content = message,
            Topic = NoteTopic
        };

        await _pendingNotes.AddAsync(note, cancellationToken);
        return note;
    }

    /// <summary>
    /// Marks a stashed note delivered after the live send, so the assistant never relays it a second
    /// time. A failure here is logged and swallowed: the user already has the message.
    /// </summary>
    private async Task AcknowledgeStashedNoteAsync(PendingUserNote? note, CancellationToken cancellationToken)
    {
        if (note?.UserId is not { } userId)
        {
            return;
        }

        try
        {
            await _pendingNotes.MarkDeliveredAsync(note.AgentId, userId, new[] { note.Id }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Pending note {NoteId} was delivered live but could not be marked delivered",
                note.Id);
        }
    }

    private static string BuildMessage(
        ReceivedEmail email, EmailAnalysis analysis, EmailActionOutcome? actionOutcome, string? periodLoadSummary)
    {
        var sender = string.IsNullOrWhiteSpace(email.FromName)
            ? email.FromAddress
            : $"{email.FromName} ({email.FromAddress})";

        var builder = new StringBuilder();
        builder.AppendLine($"📧 **{IntentLabel(analysis.Intent)}** — {sender}");
        builder.AppendLine($"Subject: {email.Subject}");
        if (analysis.FromDate != null)
        {
            var range = analysis.UntilDate != null && analysis.UntilDate != analysis.FromDate
                ? $"{analysis.FromDate:yyyy-MM-dd} – {analysis.UntilDate:yyyy-MM-dd}"
                : $"{analysis.FromDate:yyyy-MM-dd}";
            builder.AppendLine($"Period: {range}");
        }

        if (analysis.StartHour != null || analysis.EndHour != null)
        {
            builder.AppendLine($"Hours: {analysis.StartHour ?? 0}-{analysis.EndHour ?? 23}");
        }

        if (!string.IsNullOrWhiteSpace(analysis.Weekdays))
        {
            builder.AppendLine($"Weekdays: {FormatWeekdays(analysis.Weekdays)}");
        }

        if (!string.IsNullOrWhiteSpace(analysis.ScheduleCommands))
        {
            builder.AppendLine($"Planning commands: {analysis.ScheduleCommands.Replace(",", ", ")}");
        }

        builder.AppendLine();
        builder.Append(analysis.Summary);

        if (actionOutcome != null)
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append(actionOutcome.Executed ? "✅ " : "💡 ");
            builder.Append(actionOutcome.Description);
        }

        if (!string.IsNullOrWhiteSpace(periodLoadSummary))
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append(periodLoadSummary);
        }

        return builder.ToString().Trim();
    }

    private static string IntentLabel(EmailIntent intent) => intent switch
    {
        EmailIntent.CustomerMessage => "Customer email",
        EmailIntent.WorkCancellation => "Work cancellation",
        EmailIntent.VacationRequest => "Vacation request",
        EmailIntent.DayOffWish => "Day-off wish",
        EmailIntent.AvailabilityAnnouncement => "Availability announcement",
        EmailIntent.ShiftPreference => "Shift preference",
        _ => "Email received"
    };

    private static string FormatWeekdays(string weekdays)
    {
        var labels = new Dictionary<string, string>
        {
            ["1"] = "Mon", ["2"] = "Tue", ["3"] = "Wed", ["4"] = "Thu",
            ["5"] = "Fri", ["6"] = "Sat", ["7"] = "Sun"
        };

        var parts = weekdays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => labels.GetValueOrDefault(token, token));

        return string.Join(", ", parts);
    }
}
