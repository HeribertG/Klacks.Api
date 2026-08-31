// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IProactiveReminderService"/> - the reminder sweep of package F ("repeat until
/// acknowledged"). It walks the dispatch rows whose NextReminderAtUtc has fallen due and re-delivers
/// each one while its user stays silent, because acknowledgement (AcknowledgedAtUtc) is the only stop
/// truth of the loop.
///
/// Order of the gates per row, each of which exists for a different failure it prevents:
/// (1) the condition-ledger row must still exist and be open - a finding that was resolved, rejected,
///     executed or escalated has nothing left to nag about, so the row is taken out of the loop
///     (NextReminderAtUtc = null) WITHOUT another delivery;
/// (2) the per-user preference (mute / snooze / minimum severity) and the per-user sweep cap defer the
///     row to its next backoff step WITHOUT counting a reminder, so a muted user is not nagged and a
///     single busy inbox cannot drain a whole sweep;
/// (3) the compare-and-swap TryAdvanceReminderAsync is the claim: it lands only when the row still
///     carries the due date this sweep read and is not acknowledged, so a concurrent acknowledge or a
///     second sweep instance can never double-send.
///
/// Persist BEFORE push, always. The row is advanced (ReminderCount + 1, LastRemindedAtUtc, next due
/// date) before any SignalR delivery is attempted, and a failed delivery does NOT roll the advance
/// back: the reminder happened from the user's perspective the moment the row resurfaced as unread,
/// and rolling back would invite a second send. The delivery itself mirrors
/// AgentTriggerService.DeliverAsync - a connected user is live-pushed when the row is loud
/// (high severity, not in an active conversation), otherwise receives the lightweight inbox-changed
/// signal with the fresh unread count; an offline user gets nothing live, which the persisted row
/// already covers.
/// </summary>
/// <param name="dispatchRepository">Due-row reads, the reminder compare-and-swaps and the unread count.</param>
/// <param name="conditionRepository">Resolves the ledger row a reminder reports, so a closed finding stops the loop.</param>
/// <param name="preferenceService">Per-user mute / snooze / severity threshold, re-checked on every reminder.</param>
/// <param name="notificationService">Pushes the reminder and inbox changes via SignalR.</param>
/// <param name="activityTracker">Suppresses the live push while the user is actively chatting.</param>
/// <param name="timeProvider">Clock the reminded-at stamp and the next due date are taken from, injected so a test can drive it.</param>
/// <param name="logger">Structured log per row outcome and per sweep.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class ProactiveReminderService : IProactiveReminderService
{
    private static readonly ProactiveReminderSweepResult EmptyResult = new(0, 0, 0, 0, 0);

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IAgentTriggerPreferenceService _preferenceService;
    private readonly IAssistantNotificationService _notificationService;
    private readonly IUserActivityTracker _activityTracker;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProactiveReminderService> _logger;

    public ProactiveReminderService(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentConditionRepository conditionRepository,
        IAgentTriggerPreferenceService preferenceService,
        IAssistantNotificationService notificationService,
        IUserActivityTracker activityTracker,
        TimeProvider timeProvider,
        ILogger<ProactiveReminderService> logger)
    {
        _dispatchRepository = dispatchRepository;
        _conditionRepository = conditionRepository;
        _preferenceService = preferenceService;
        _notificationService = notificationService;
        _activityTracker = activityTracker;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ProactiveReminderSweepResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var dueRows = await _dispatchRepository.GetDueForReminderAsync(
            nowUtc, ProactiveReminderDefaults.SweepBatchSize, cancellationToken);

        if (dueRows.Count == 0)
        {
            return EmptyResult;
        }

        var connectedLookup = BuildConnectedLookup(await _notificationService.GetConnectedUserIdsAsync());
        var tally = new SweepTally { Due = dueRows.Count };
        var remindersPerUser = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in dueRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessRowAsync(row, nowUtc, connectedLookup, remindersPerUser, tally, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One broken row must not cost the rest of the batch. The row keeps the due date it
                // had, so the next sweep simply retries it.
                _logger.LogError(
                    ex,
                    "Reminder sweep failed for dispatch row {RowId}; the row keeps its due date and is retried by the next sweep",
                    row.Id);
            }
        }

        var result = tally.ToResult();
        _logger.LogInformation(
            "Proactive reminder sweep: {Due} due, {Reminded} reminded, {Stopped} stopped, {Skipped} skipped, {Lost} lost",
            result.Due, result.Reminded, result.Stopped, result.Skipped, result.Lost);

        return result;
    }

    private async Task ProcessRowAsync(
        ProactiveTriggerDispatchRow row,
        DateTime nowUtc,
        IReadOnlyDictionary<string, string> connectedLookup,
        IDictionary<string, int> remindersPerUser,
        SweepTally tally,
        CancellationToken cancellationToken)
    {
        // GetDueForReminderAsync only returns rows whose due date is set; a row without one cannot be
        // compare-and-swapped at all, so there is nothing to do but leave it alone.
        if (row.NextReminderAtUtc is not { } dueUtc)
        {
            return;
        }

        var condition = row.ConditionId is Guid conditionId
            ? await _conditionRepository.GetByIdAsync(conditionId, cancellationToken)
            : null;

        if (condition is null || !AgentConditionStateMachine.IsOpen(condition.Status))
        {
            // The finding is gone or terminal - stop the loop WITHOUT another delivery.
            await _dispatchRepository.TryRescheduleReminderAsync(row.Id, dueUtc, null, cancellationToken);
            tally.Stopped++;
            return;
        }

        remindersPerUser.TryGetValue(row.UserId, out var remindersThisSweep);
        if (remindersThisSweep >= ProactiveReminderDefaults.MaxRemindersPerUserPerSweep
            || !await _preferenceService.IsAllowedAsync(row.UserId, row.TriggerKind, row.Severity ?? AgentTriggerSeverity.Low))
        {
            // Deferred WITHOUT counting a reminder: mute / snooze / minimum severity and the per-user
            // cap move the due date forward but must not burn a backoff step.
            await _dispatchRepository.TryRescheduleReminderAsync(
                row.Id, dueUtc, ProactiveReminderSchedule.NextDueAfter(row.ReminderCount, nowUtc), cancellationToken);
            tally.Skipped++;
            return;
        }

        var nextDueUtc = ProactiveReminderSchedule.NextDueAfter(row.ReminderCount + 1, nowUtc);
        if (!await _dispatchRepository.TryAdvanceReminderAsync(row.Id, dueUtc, nowUtc, nextDueUtc, cancellationToken))
        {
            // The claim lost: the user acknowledged the row or another instance's sweep got there
            // first. Either way there is nothing to deliver from THIS run.
            tally.Lost++;
            return;
        }

        tally.Reminded++;
        remindersPerUser[row.UserId] = remindersThisSweep + 1;

        if (!connectedLookup.TryGetValue(row.UserId, out var deliveryUserId))
        {
            // Offline users get nothing live; the advance above already resurfaced the row as unread,
            // which is the whole delivery an offline user ever gets.
            return;
        }

        await DeliverAsync(row, deliveryUserId, cancellationToken);
    }

    /// <summary>
    /// The live half of one reminder, mirroring AgentTriggerService.DeliverAsync: loud rows (high
    /// severity, user not in an active conversation) go straight into the chat, everything else only
    /// nudges the inbox badge. A failed push is a warning, never a rollback - the row was already
    /// advanced, and rolling back would invite a duplicate reminder.
    /// </summary>
    private async Task DeliverAsync(
        ProactiveTriggerDispatchRow row,
        string deliveryUserId,
        CancellationToken cancellationToken)
    {
        if (ProactiveLivePushPolicy.ShouldLivePushReminder(row.Severity, _activityTracker, deliveryUserId))
        {
            try
            {
                await _notificationService.SendProactiveMessageAsync(
                    deliveryUserId,
                    FormatReminderMessage(row),
                    contentParams: ParseParams(row.ContentParamsJson),
                    messageId: row.Id.ToString(),
                    kind: row.TriggerKind,
                    actionRoute: row.ActionRoute,
                    actionParams: ParseParams(row.ActionParamsJson));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Reminder live push failed for user {UserId}; the row was already advanced and stays reachable via the inbox",
                    row.UserId);
            }

            return;
        }

        try
        {
            var unreadCount = await _dispatchRepository.CountUnreadAsync(row.UserId, cancellationToken);
            await _notificationService.SendProactiveInboxChangedAsync(deliveryUserId, unreadCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reminder inbox-changed signal failed for user {UserId}", row.UserId);
        }
    }

    /// <summary>
    /// Same rendering AgentTriggerService.FormatMessage applies at first dispatch: an i18n key passes
    /// through untouched, plain text carries its severity tag. Stored on the row is only the content
    /// key, so the tag is re-derived from the row's severity.
    /// </summary>
    private static string FormatReminderMessage(ProactiveTriggerDispatchRow row)
    {
        var contentKey = row.ContentKey ?? string.Empty;
        if (contentKey.StartsWith(ProactiveMessageMarkers.I18nPrefix, StringComparison.Ordinal))
        {
            return contentKey;
        }

        var severityTag = row.Severity switch
        {
            AgentTriggerSeverity.High => "[HIGH] ",
            AgentTriggerSeverity.Medium => "[MEDIUM] ",
            _ => ""
        };

        return $"{severityTag}{contentKey}";
    }

    /// <summary>
    /// The row stores its parameter dictionaries as JSON; a payload that no longer parses degrades to
    /// no parameters rather than failing the row's delivery.
    /// </summary>
    private IReadOnlyDictionary<string, string>? ParseParams(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Dispatch row carries parameters that are not valid JSON; the reminder goes out without them");
            return null;
        }
    }

    private static Dictionary<string, string> BuildConnectedLookup(IReadOnlyList<string> connectedUserIds)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var connectedUserId in connectedUserIds)
        {
            lookup.TryAdd(connectedUserId, connectedUserId);
        }

        return lookup;
    }

    private sealed class SweepTally
    {
        public int Due { get; set; }

        public int Reminded { get; set; }

        public int Stopped { get; set; }

        public int Skipped { get; set; }

        public int Lost { get; set; }

        public ProactiveReminderSweepResult ToResult() => new(Due, Reminded, Stopped, Skipped, Lost);
    }
}
