// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Dispatches proactive trigger events to their audience. The recipient set is derived from the
/// event itself (target user, planner/admin audience, or the currently connected users for
/// companion broadcasts), not from who happens to be online. Per recipient the pipeline is:
/// preference check (mute / snooze / minimum-severity), persisted dedup, daily rate-limit — then
/// the event is ALWAYS persisted as an inbox row (also for offline users). A live chat push via
/// SignalR happens only for connected recipients when the severity is high or the event is a
/// companion trigger; all other connected recipients receive a lightweight inbox-changed signal.
/// Recipients without a connection used to end there, which made SignalR the only loud channel and
/// left anyone who is offline — the night-time planner above all — unreachable. An event that
/// MessengerWakeUpPolicy admits now additionally goes out over the recipient's preferred messenger.
/// That gate is narrower than the live-push gate on purpose, because it interrupts somebody who is
/// not at work. The path is strictly additive: the inbox row is written before it and is never
/// replaced by it, and a recipient without a messenger identity is left exactly as before.
/// </summary>
/// <param name="rateLimiter">Per-user-per-kind daily budget gate.</param>
/// <param name="preferenceService">Per-user mute / snooze / severity threshold.</param>
/// <param name="notificationService">Pushes proactive messages and inbox changes via SignalR.</param>
/// <param name="dispatchRepository">Persists dispatch rows serving as dedup log and inbox.</param>
/// <param name="conditionRepository">Resolves the condition-ledger row a ledger-tracked event reports, so a later dismissal can write its reject reason back onto the finding.</param>
/// <param name="activityTracker">Suppresses live pushes while the user is actively chatting.</param>
/// <param name="planningAudienceResolver">Resolves the planner / admin audience, narrowed to the union of the GroupVisibility scopes of every group the event names, or per named employee for a client-scoped event, whose recipients each get only the employees they may see.</param>
/// <param name="offlineMessengerNotifier">Loud channel for recipients without a live connection.</param>
/// <param name="messengerTextComposer">Renders the messenger sentence in the installation language.</param>
/// <param name="timeProvider">Clock the first reminder due date is stamped from, injected so a test can drive it.</param>
/// <param name="logger">Structured log per dispatch.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AgentTriggerService : IAgentTriggerService
{
    private const string VisibilityKeySeparator = ",";

    private readonly IAgentTriggerRateLimiter _rateLimiter;
    private readonly IAgentTriggerPreferenceService _preferenceService;
    private readonly IAssistantNotificationService _notificationService;
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IUserActivityTracker _activityTracker;
    private readonly IPlanningAudienceResolver _planningAudienceResolver;
    private readonly IOfflineMessengerNotifier _offlineMessengerNotifier;
    private readonly IProactiveMessengerTextComposer _messengerTextComposer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AgentTriggerService> _logger;

    public AgentTriggerService(
        IAgentTriggerRateLimiter rateLimiter,
        IAgentTriggerPreferenceService preferenceService,
        IAssistantNotificationService notificationService,
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentConditionRepository conditionRepository,
        IUserActivityTracker activityTracker,
        IPlanningAudienceResolver planningAudienceResolver,
        IOfflineMessengerNotifier offlineMessengerNotifier,
        IProactiveMessengerTextComposer messengerTextComposer,
        TimeProvider timeProvider,
        ILogger<AgentTriggerService> logger)
    {
        _rateLimiter = rateLimiter;
        _preferenceService = preferenceService;
        _notificationService = notificationService;
        _dispatchRepository = dispatchRepository;
        _conditionRepository = conditionRepository;
        _activityTracker = activityTracker;
        _planningAudienceResolver = planningAudienceResolver;
        _offlineMessengerNotifier = offlineMessengerNotifier;
        _messengerTextComposer = messengerTextComposer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ProactiveDispatchOutcome> OnEventAsync(IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken = default)
    {
        var connectedUserIds = (await _notificationService.GetConnectedUserIdsAsync()).ToList();
        var variants = await ResolveRecipientVariantsAsync(triggerEvent, connectedUserIds, cancellationToken);
        if (variants.Count == 0)
        {
            _logger.LogDebug("Trigger {Kind} skipped — no recipients", triggerEvent.Kind);
            return ProactiveDispatchOutcome.Empty;
        }

        var connectedLookup = BuildConnectedLookup(connectedUserIds);

        // Capped once for the whole event, never per recipient: the dedup probe below must ask for
        // exactly the key that gets stored, or a capped row would never be recognised again and the
        // same alert would be re-sent on every scan. Taken from the ORIGINAL event, never from a
        // recipient's narrowed variant, so every variant reports the same ledger row and dedups alike.
        var contentKey = ProactiveTextTruncator.Cap(triggerEvent.Summary, ProactiveTriggerDispatchLimits.ContentKeyMaxLength);
        var dedupKey = ProactiveTextTruncator.Cap(triggerEvent.DedupKey, ProactiveTriggerDispatchLimits.DedupKeyMaxLength) ?? string.Empty;
        var conditionId = await ResolveConditionIdAsync(triggerEvent, cancellationToken);
        // Stamped once per event from the injected clock - never from the row's CreateTime, which
        // DataBaseContext.OnBeforeSaving fills from the system clock at save time instead.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var tally = new DispatchTally();

        foreach (var variant in variants)
        {
            await DispatchVariantAsync(
                variant, contentKey, dedupKey, conditionId, now, connectedLookup, tally, cancellationToken);
        }

        _logger.LogInformation(
            "Trigger {Kind} severity={Severity} persisted for {Persisted} user(s) ({LivePushed} live, {InboxSignaled} inbox-signaled, {MessengerSent} messenger), {Throttled} throttled, {Muted} muted, {Deduped} deduped, {Failed} failed. Summary: {Summary}",
            triggerEvent.Kind, triggerEvent.Severity, tally.Persisted, tally.LivePushed, tally.InboxSignaled, tally.MessengerSent,
            tally.Throttled, tally.Muted, tally.Deduped, tally.Failed, triggerEvent.Summary);

        return new ProactiveDispatchOutcome(tally.Persisted, tally.Throttled, tally.Muted, tally.Deduped, tally.Failed);
    }

    /// <summary>
    /// Delivers one recipient variant. Everything a recipient READS - the frozen content parameters, the
    /// severity (which the preference gate and the reminder sweep evaluate), the live-push text and the
    /// messenger sentence - comes from the variant, which for a client-scoped event is narrowed to the
    /// employees these recipients may see. Only the keys that identify the finding (content key, dedup
    /// key, condition id) are shared across variants.
    /// </summary>
    private async Task DispatchVariantAsync(
        RecipientVariant variant,
        string? contentKey,
        string dedupKey,
        Guid? conditionId,
        DateTime now,
        IReadOnlyDictionary<string, string> connectedLookup,
        DispatchTally tally,
        CancellationToken cancellationToken)
    {
        var variantEvent = variant.Event;
        var message = FormatMessage(variantEvent);

        // The wake-up decision depends on the event alone, never on the recipient, so it is taken
        // once per variant here. A null text is the carrier of "this event may not wake anybody" and
        // keeps the language lookup out of the per-recipient loop.
        var messengerText = MessengerWakeUpPolicy.JustifiesWakingSomebody(variantEvent.Kind, variantEvent.Severity)
            ? await ComposeMessengerTextAsync(variantEvent, cancellationToken)
            : null;

        var contentParamsJson = BuildCappedParamsJson(variantEvent.SummaryParams, ProactiveTriggerDispatchLimits.ContentParamsJsonMaxLength);
        var actionParamsJson = BuildCappedParamsJson(variantEvent.ActionParams, ProactiveTriggerDispatchLimits.ActionParamsJsonMaxLength);

        foreach (var userId in variant.UserIds)
        {
            if (!await _preferenceService.IsAllowedAsync(userId, variantEvent.Kind, variantEvent.Severity))
            {
                tally.Muted++;
                continue;
            }

            if (await _dispatchRepository.WasDispatchedAsync(userId, variantEvent.Kind, dedupKey, conditionId, cancellationToken))
            {
                tally.Deduped++;
                continue;
            }

            if (!_rateLimiter.ShouldFire(userId, variantEvent.Kind))
            {
                tally.Throttled++;
                continue;
            }

            var messageId = Guid.NewGuid();
            try
            {
                // Persist BEFORE any live delivery: inbox rows must exist even for offline users,
                // and a failed live push must not lose the message.
                await _dispatchRepository.RecordAsync(new ProactiveTriggerDispatchRow
                {
                    Id = messageId,
                    UserId = userId,
                    TriggerKind = variantEvent.Kind,
                    DedupKey = dedupKey,
                    ContentKey = contentKey,
                    ContentParamsJson = contentParamsJson,
                    Severity = variantEvent.Severity,
                    ActionRoute = variantEvent.ActionRoute,
                    ActionParamsJson = actionParamsJson,
                    ConditionId = conditionId,
                    // Only condition-linked rows join the reminder loop; everything else stays a
                    // plain inbox message that never re-fires.
                    NextReminderAtUtc = conditionId is null ? null : ProactiveReminderSchedule.FirstDueAfter(now)
                }, cancellationToken);
                _rateLimiter.RecordFire(userId, variantEvent.Kind);
                tally.Persisted++;
            }
            catch (Exception ex)
            {
                // Error, not warning: a failed live push or messenger send still leaves the message
                // readable in the inbox, but a failed row means this recipient never learns of the
                // event at all. The loop still continues so one bad row cannot cost the remaining
                // recipients theirs.
                tally.Failed++;
                _logger.LogError(ex, "Trigger {Kind} persistence failed for user {UserId}; the notification is lost for this recipient", variantEvent.Kind, userId);
                continue;
            }

            if (!connectedLookup.TryGetValue(userId, out var deliveryUserId))
            {
                if (messengerText != null
                    && await TryReachOfflineRecipientAsync(variantEvent, userId, messengerText, cancellationToken))
                {
                    tally.MessengerSent++;
                }

                continue;
            }

            var (wasLivePushed, wasInboxSignaled) = await DeliverAsync(variantEvent, userId, deliveryUserId, message, messageId, cancellationToken);
            if (wasLivePushed)
            {
                tally.LivePushed++;
            }

            if (wasInboxSignaled)
            {
                tally.InboxSignaled++;
            }
        }
    }

    /// <summary>
    /// Links this event's dispatch rows to the condition-ledger row it reports, so a dismissal months
    /// later still knows which finding was rejected. Resolved once per event rather than per recipient:
    /// the fingerprint depends on the event alone, and every recipient of one event reports the same
    /// finding. Kept as a lookup by fingerprint instead of a value handed down from the tick, because
    /// the same linkage has to work for the dispatch rows other call sites write.
    ///
    /// Null is the ordinary answer for everything the ledger does not track (companion broadcasts,
    /// per-user events, anything posted outside the trigger tick) and also for a tracked event whose
    /// row another instance has meanwhile closed. A lookup failure degrades to null as well: the
    /// notification itself matters more than its provenance link, so it is never worth losing.
    /// </summary>
    private async Task<Guid?> ResolveConditionIdAsync(
        IAgentTriggerEvent triggerEvent,
        CancellationToken cancellationToken)
    {
        if (!AgentConditionLedgerPolicy.IsLedgerTracked(triggerEvent))
        {
            return null;
        }

        try
        {
            var condition = await _conditionRepository.FindOpenByFingerprintAsync(
                AgentConditionLedgerPolicy.FingerprintFor(triggerEvent),
                cancellationToken);

            return condition?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Trigger {Kind} could not be linked to its condition-ledger row; the dispatch rows are written without a condition reference",
                triggerEvent.Kind);

            return null;
        }
    }

    /// <summary>
    /// Second delivery path for a recipient without a live connection. The gate here is
    /// MessengerWakeUpPolicy and NOT ProactiveLivePushPolicy.IsLoudEvent: "loud" governs a chat push to somebody who is
    /// already sitting in front of Klacks, which is a far cheaper interruption than a message on
    /// the phone of somebody who is asleep. Everything the policy does not admit stops here and
    /// stays readable in the inbox. Per-recipient mute, snooze and minimum-severity have already
    /// been enforced by OnEventAsync before this point, so a muted recipient can never arrive here.
    /// A refused send is logged at warning level (decision E47): Klacksy owes nobody a read
    /// receipt and could not produce one, but the provider does report a blocked bot or a dead
    /// channel, and a report that swallows that would assert an alert which never went out.
    /// </summary>
    private async Task<bool> TryReachOfflineRecipientAsync(
        IAgentTriggerEvent triggerEvent,
        string userId,
        string messengerText,
        CancellationToken cancellationToken)
    {
        OfflineMessengerDeliveryResult result;
        try
        {
            result = await _offlineMessengerNotifier.TrySendAsync(
                userId,
                messengerText,
                triggerEvent.Kind,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // The inbox row for this recipient is already persisted; what must not happen is that a
            // broken loud channel aborts the loop and costs the remaining recipients their rows.
            _logger.LogWarning(
                ex, "Trigger {Kind} messenger delivery threw for offline user {UserId}", triggerEvent.Kind, userId);
            return false;
        }

        switch (result.Outcome)
        {
            case OfflineMessengerDeliveryOutcome.Sent:
                _logger.LogInformation(
                    "Trigger {Kind} reached offline user {UserId} over {Channel}",
                    triggerEvent.Kind, userId, result.Channel);
                return true;

            case OfflineMessengerDeliveryOutcome.Failed:
                _logger.LogWarning(
                    "Trigger {Kind} could NOT be sent to offline user {UserId} over {Channel}: {Error}. The message stays reachable via the inbox only",
                    triggerEvent.Kind, userId, result.Channel, result.ErrorMessage);
                return false;

            case OfflineMessengerDeliveryOutcome.Throttled:
                _logger.LogWarning(
                    "Trigger {Kind} was rate-limited by {Channel} for offline user {UserId}: {Error}. The message stays reachable via the inbox only",
                    triggerEvent.Kind, result.Channel, userId, result.ErrorMessage);
                return false;

            case OfflineMessengerDeliveryOutcome.NoContact:
                _logger.LogInformation(
                    "Trigger {Kind} has no messenger identity for offline user {UserId}; inbox row written, no loud channel",
                    triggerEvent.Kind, userId);
                return false;

            default:
                _logger.LogDebug(
                    "Trigger {Kind} found no messenger channel for offline user {UserId}",
                    triggerEvent.Kind, userId);
                return false;
        }
    }

    private async Task<(bool LivePushed, bool InboxSignaled)> DeliverAsync(
        IAgentTriggerEvent triggerEvent,
        string userId,
        string deliveryUserId,
        string message,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        if (ProactiveLivePushPolicy.ShouldLivePush(triggerEvent, _activityTracker, deliveryUserId))
        {
            try
            {
                await _notificationService.SendProactiveMessageAsync(
                    deliveryUserId,
                    message,
                    contentParams: triggerEvent.SummaryParams,
                    messageId: messageId.ToString(),
                    kind: triggerEvent.Kind,
                    actionRoute: triggerEvent.ActionRoute,
                    actionParams: triggerEvent.ActionParams);
                return (true, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trigger {Kind} live push failed for user {UserId}; the message stays reachable via the inbox", triggerEvent.Kind, userId);
                return (false, false);
            }
        }

        try
        {
            var unreadCount = await _dispatchRepository.CountUnreadAsync(userId, cancellationToken);
            await _notificationService.SendProactiveInboxChangedAsync(deliveryUserId, unreadCount);
            return (false, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trigger {Kind} inbox-changed signal failed for user {UserId}", triggerEvent.Kind, userId);
            return (false, false);
        }
    }

    /// <summary>
    /// The recipients of one event, each paired with the variant of the event they may read. Every event
    /// except a client-scoped planner event yields exactly one variant - the event itself - for its whole
    /// audience. A client-scoped planner event (IClientScopedTriggerEvent) names employees, so its audience
    /// follows the client group-visibility rule per named employee instead, see
    /// <see cref="ResolveClientScopedVariantsAsync"/>.
    /// </summary>
    private async Task<IReadOnlyList<RecipientVariant>> ResolveRecipientVariantsAsync(
        IAgentTriggerEvent triggerEvent,
        IReadOnlyList<string> connectedUserIds,
        CancellationToken cancellationToken)
    {
        if (triggerEvent.TargetUserId is null
            && !triggerEvent.AdminOnly
            && triggerEvent.PlannersOnly
            && triggerEvent is IClientScopedTriggerEvent clientScopedEvent)
        {
            return await ResolveClientScopedVariantsAsync(clientScopedEvent, cancellationToken);
        }

        var recipients = await ResolveRecipientsAsync(triggerEvent, connectedUserIds, cancellationToken);
        return recipients.Count == 0
            ? []
            : [new RecipientVariant(triggerEvent, recipients)];
    }

    /// <summary>
    /// Per affected employee, the planners who may see that employee
    /// (IPlanningAudienceResolver.GetPlanningUserIdsForClientAsync: Admins always; every planner for an
    /// employee without any group; otherwise the planners of the employee's groups; Admins only for an
    /// unknown, deleted or scenario-only employee). Each recipient then receives the event narrowed to
    /// exactly the employees they may see, and nothing when that set is empty - there is no unscoped
    /// fallback, because the content is the employees' names. Recipients with an identical visible set
    /// share one variant, keyed on the sorted ids, so the narrowing and the messenger text are built once
    /// per distinct view rather than once per recipient. One membership read per affected employee per
    /// call; the group audiences underneath are cached per Nested Set root.
    /// </summary>
    private async Task<IReadOnlyList<RecipientVariant>> ResolveClientScopedVariantsAsync(
        IClientScopedTriggerEvent clientScopedEvent,
        CancellationToken cancellationToken)
    {
        var visibleClientsByUser = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var clientId in clientScopedEvent.AffectedClientIds.Distinct())
        {
            var audience = await _planningAudienceResolver.GetPlanningUserIdsForClientAsync(clientId, cancellationToken);
            foreach (var userId in audience)
            {
                if (!visibleClientsByUser.TryGetValue(userId, out var visibleClients))
                {
                    visibleClients = [];
                    visibleClientsByUser[userId] = visibleClients;
                }

                visibleClients.Add(clientId);
            }
        }

        var variants = new List<RecipientVariant>();
        foreach (var bucket in visibleClientsByUser.GroupBy(pair => BuildVisibilityKey(pair.Value), StringComparer.Ordinal))
        {
            var narrowedEvent = clientScopedEvent.NarrowTo(bucket.First().Value);
            if (narrowedEvent is null)
            {
                continue;
            }

            variants.Add(new RecipientVariant(narrowedEvent, bucket.Select(pair => pair.Key).ToList()));
        }

        return variants;
    }

    private static string BuildVisibilityKey(IEnumerable<Guid> visibleClientIds) =>
        string.Join(VisibilityKeySeparator, visibleClientIds.OrderBy(id => id));

    private async Task<IReadOnlyList<string>> ResolveRecipientsAsync(
        IAgentTriggerEvent triggerEvent,
        IReadOnlyList<string> connectedUserIds,
        CancellationToken cancellationToken)
    {
        if (triggerEvent.TargetUserId is Guid targetUserId)
        {
            return [targetUserId.ToString()];
        }

        if (triggerEvent.AdminOnly)
        {
            var adminIds = await _planningAudienceResolver.GetAdminUserIdsAsync(cancellationToken);
            return adminIds.ToList();
        }

        if (triggerEvent.PlannersOnly)
        {
            var plannerIds = await ResolvePlannerAudienceAsync(triggerEvent, cancellationToken);
            return plannerIds.ToList();
        }

        // Companion broadcasts (curiosity / onboarding style events without an audience gate) go to
        // currently connected users only — deliberately no mass persistence for every known user.
        return connectedUserIds;
    }

    /// <summary>
    /// The planner audience of one event. A shift can belong to several groups at once, so the scoped
    /// audience is the UNION over every group the event names: a planner who may see any one of those
    /// groups may see the finding. GetPlanningUserIdsForGroupAsync already returns every Admin plus the
    /// planners scoped to that group, so the union stays admin-inclusive and is cached per Nested Set
    /// root underneath. Events that name employees never get here: they are client-scoped and resolved
    /// per employee in <see cref="ResolveClientScopedVariantsAsync"/>, so the unscoped broadcast at the
    /// end only carries content without personal data.
    /// </summary>
    private async Task<IReadOnlySet<string>> ResolvePlannerAudienceAsync(
        IAgentTriggerEvent triggerEvent,
        CancellationToken cancellationToken)
    {
        var groupIds = triggerEvent.GroupIds;
        if (groupIds.Count > 0)
        {
            var scopedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var groupId in groupIds)
            {
                scopedIds.UnionWith(await _planningAudienceResolver.GetPlanningUserIdsForGroupAsync(groupId, cancellationToken));
            }

            return scopedIds;
        }

        // An event that is only ever about a group-owned entity and still names no group means the
        // group could NOT be determined — a shift with no membership row. That is the empty case of
        // the union above, whose limit is the always-unrestricted admins, and NOT a licence to fall
        // through to the unscoped broadcast: the broadcast exists for installation-wide alerts, and
        // routing an unattributable shift finding through it would hand every planner exactly the
        // group-scoped detail the scoping above is there to withhold.
        if (triggerEvent.RequiresGroupScope)
        {
            return await _planningAudienceResolver.GetAdminUserIdsAsync(cancellationToken);
        }

        return await _planningAudienceResolver.GetPlanningUserIdsAsync(cancellationToken);
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

    private static string? BuildCappedParamsJson(IReadOnlyDictionary<string, string>? paramValues, int maxJsonLength)
    {
        if (paramValues == null)
        {
            return null;
        }

        var cappedParams = paramValues.ToDictionary(
            pair => pair.Key,
            pair => TruncateParamValue(pair.Value));
        var json = JsonSerializer.Serialize(cappedParams);
        return json.Length <= maxJsonLength ? json : null;
    }

    private static string TruncateParamValue(string value)
        => ProactiveTextTruncator.Cap(value, ProactiveTriggerDispatchLimits.ContentParamValueMaxLength) ?? string.Empty;

    /// <summary>
    /// Renders the messenger sentence once per event. A composer failure must not cost the
    /// remaining recipients their inbox rows, so it degrades to "no loud channel this round"
    /// rather than propagating out of the dispatch loop.
    /// </summary>
    private async Task<string?> ComposeMessengerTextAsync(
        IAgentTriggerEvent triggerEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _messengerTextComposer.ComposeAsync(triggerEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Trigger {Kind} messenger text could not be composed; the message stays reachable via the inbox only",
                triggerEvent.Kind);
            return null;
        }
    }

    private static string FormatMessage(IAgentTriggerEvent triggerEvent)
    {
        if (triggerEvent.Summary.StartsWith(ProactiveMessageMarkers.I18nPrefix, StringComparison.Ordinal))
        {
            return triggerEvent.Summary;
        }

        var severityTag = triggerEvent.Severity switch
        {
            AgentTriggerSeverity.High => "[HIGH] ",
            AgentTriggerSeverity.Medium => "[MEDIUM] ",
            _ => ""
        };
        return $"{severityTag}{triggerEvent.Summary}";
    }

    private sealed record RecipientVariant(IAgentTriggerEvent Event, IReadOnlyList<string> UserIds);

    private sealed class DispatchTally
    {
        public int Persisted { get; set; }

        public int Failed { get; set; }

        public int LivePushed { get; set; }

        public int InboxSignaled { get; set; }

        public int MessengerSent { get; set; }

        public int Throttled { get; set; }

        public int Muted { get; set; }

        public int Deduped { get; set; }
    }
}
