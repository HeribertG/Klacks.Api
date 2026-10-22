// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IUiExplainOfferService"/> (T5): the explain offer the UI shows when the user does something whose
/// meaning the UI knows, for now the first tick of FT or +FT on a shift. A topic may be offered when explain offers
/// are on (setting KLACKSY_EXPLAIN_OFFERS_ENABLED not "false", shared with the T4 gate offers, and the proactive kill
/// switch off), the user's explanation level is one of UiExplainOfferDefaults.OfferedLevels, and the user has not muted
/// the topic's kind. Every reaction is one counter row in agent_trigger_dispatches (kind "explain.ui.&lt;key&gt;",
/// no ContentKey so it never reaches the inbox or the reminder sweep, ContentParamsJson carries only the outcome); the
/// dosage itself lives in the user's per-kind mute, which survives the retention of the counter rows: yes or no mutes
/// at once, the second "later" mutes, and so does the third time the offer was shown at all. The rows are written
/// directly, not through SetProactiveReactionCommandHandler, whose dismiss-streak evaluators would read a "later" as a
/// rejected notification. Writing is gated like reading: while the offers are switched off nothing is stored, and while
/// the topic is muted or snoozed for the user a reaction is accepted (idempotent) but stores nothing - with one
/// exception: the answer (accepted, later, declined) to the very offer whose "shown" muted the topic is still stored as
/// long as the user's newest row of the kind is that unanswered "shown". The UI sends "shown" before the toast appears,
/// so without it the answer to the third offer would be lost; at most one answer per offer, so a client still cannot
/// grow the counter table of a topic it may no longer be offered. Reading is fail-closed: any failure means "do not offer". Logs never
/// carry the user. A passive topic (UiExplainOfferTopic.IsPassive, a link the UI always shows) is never offered by state
/// (always off), stores only "accepted", at most one row per user and company-local day (the DedupKey carries the day from
/// ICompanyClock; RecordAsync skips an existing key and the unique dedup index backs a concurrent duplicate), and never
/// touches the mute.
/// </summary>
/// <param name="dispatches">Counter rows of the offers</param>
/// <param name="preferences">The user's per-kind mute; the dosage is written here</param>
/// <param name="offersSwitch">The explain-offers setting and the proactive kill switch, shared with the gate offers</param>
/// <param name="explainDepthResolver">Resolves the user's explanation level (beginner/advanced)</param>
/// <param name="timeProvider">Clock for the reaction stamp</param>
/// <param name="companyClock">Company-local day of a passive click row</param>
/// <param name="logger">Reports a swallowed failure</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.ExplainOffers;

public sealed class UiExplainOfferService : IUiExplainOfferService
{
    private const string GuidFormat = "N";

    private readonly IProactiveTriggerDispatchRepository _dispatches;
    private readonly IAgentTriggerPreferenceService _preferences;
    private readonly IExplainOffersSwitch _offersSwitch;
    private readonly IExplainDepthResolver _explainDepthResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<UiExplainOfferService> _logger;

    public UiExplainOfferService(
        IProactiveTriggerDispatchRepository dispatches,
        IAgentTriggerPreferenceService preferences,
        IExplainOffersSwitch offersSwitch,
        IExplainDepthResolver explainDepthResolver,
        TimeProvider timeProvider,
        ICompanyClock companyClock,
        ILogger<UiExplainOfferService> logger)
    {
        _dispatches = dispatches;
        _preferences = preferences;
        _offersSwitch = offersSwitch;
        _explainDepthResolver = explainDepthResolver;
        _timeProvider = timeProvider;
        _companyClock = companyClock;
        _logger = logger;
    }

    public async Task<UiExplainOfferState> GetStateAsync(
        string userId, string topicKey, CancellationToken cancellationToken = default)
    {
        var topic = UiExplainOfferTopics.Find(topicKey);
        if (topic == null || topic.IsPassive || string.IsNullOrWhiteSpace(userId))
        {
            return UiExplainOfferState.Off;
        }

        try
        {
            if (!await _offersSwitch.IsOnAsync(cancellationToken))
            {
                return UiExplainOfferState.Off;
            }

            var shouldOffer = await IsLevelOfferedAsync(userId, cancellationToken)
                && await _preferences.IsAllowedAsync(userId, UiExplainOfferTopics.TriggerKindOf(topic), AgentTriggerSeverity.Low);
            return new UiExplainOfferState(Enabled: true, ShouldOffer: shouldOffer);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Reading the UI explain offer state of topic {TopicKey} failed", topic.Key);
            return UiExplainOfferState.Off;
        }
    }

    public async Task<bool> RecordReactionAsync(
        string userId, string topicKey, string outcome, CancellationToken cancellationToken = default)
    {
        var topic = UiExplainOfferTopics.Find(topicKey);
        if (topic == null || !UiExplainOfferOutcomes.IsKnown(outcome) || string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var kind = UiExplainOfferTopics.TriggerKindOf(topic);
        if (!await _offersSwitch.IsOnAsync(cancellationToken))
        {
            return true;
        }

        if (topic.IsPassive)
        {
            await RecordPassiveClickAsync(userId, topic, kind, outcome, cancellationToken);
            return true;
        }

        if (!await _preferences.IsAllowedAsync(userId, kind, AgentTriggerSeverity.Low))
        {
            if (await AnswersTheMutingOfferAsync(userId, kind, outcome, cancellationToken))
            {
                await _dispatches.RecordAsync(NewRow(userId, kind, outcome, UniqueDedupKey(topic, outcome)), cancellationToken);
            }

            return true;
        }

        await _dispatches.RecordAsync(NewRow(userId, kind, outcome, UniqueDedupKey(topic, outcome)), cancellationToken);

        if (await ShouldMuteAfterAsync(userId, kind, outcome, cancellationToken))
        {
            await _preferences.MuteAsync(userId, kind, true);
        }

        return true;
    }

    private async Task RecordPassiveClickAsync(
        string userId, UiExplainOfferTopic topic, string kind, string outcome, CancellationToken cancellationToken)
    {
        if (outcome != UiExplainOfferOutcomes.Accepted)
        {
            return;
        }

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var row = NewRow(userId, kind, outcome,
            topic.Key + UiExplainOfferDefaults.DedupKeySeparator + outcome + UiExplainOfferDefaults.DedupKeySeparator
                + today.ToString(UiExplainOfferDefaults.PassiveDayFormat, CultureInfo.InvariantCulture));
        if (!await _dispatches.TryRecordAsync(row, cancellationToken))
        {
            _logger.LogDebug("A concurrent click of the passive UI explain topic {TopicKey} was already recorded", topic.Key);
        }
    }

    private async Task<bool> AnswersTheMutingOfferAsync(
        string userId, string kind, string outcome, CancellationToken cancellationToken)
    {
        if (outcome == UiExplainOfferOutcomes.Shown)
        {
            return false;
        }

        var newest = await _dispatches.FindNewestAsync(userId, kind, cancellationToken);
        return newest is { Reaction: ProactiveReaction.None }
            && ExplainOfferMarker.Parse(newest.ContentParamsJson).Outcome == UiExplainOfferOutcomes.Shown;
    }

    private async Task<bool> IsLevelOfferedAsync(string userId, CancellationToken cancellationToken)
    {
        var depth = await _explainDepthResolver.ResolveAsync(userId, cancellationToken);
        return depth != null && UiExplainOfferDefaults.OfferedLevels.Contains(depth.Level);
    }

    private async Task<bool> ShouldMuteAfterAsync(
        string userId, string kind, string outcome, CancellationToken cancellationToken)
    {
        switch (outcome)
        {
            case UiExplainOfferOutcomes.Accepted:
            case UiExplainOfferOutcomes.Declined:
                return true;
            case UiExplainOfferOutcomes.Later:
                var recent = await _dispatches.GetRecentReactionsAsync(
                    userId, kind, UiExplainOfferDefaults.MaxLaterBeforeMute, cancellationToken);
                return recent.Count(row => ExplainOfferMarker.Parse(row.ContentParamsJson).Outcome == UiExplainOfferOutcomes.Later)
                    >= UiExplainOfferDefaults.MaxLaterBeforeMute;
            default:
                return await _dispatches.CountUnreactedAsync(userId, kind, cancellationToken)
                    >= UiExplainOfferDefaults.MaxShownBeforeMute;
        }
    }

    private static string UniqueDedupKey(UiExplainOfferTopic topic, string outcome) =>
        topic.Key + UiExplainOfferDefaults.DedupKeySeparator + outcome
            + UiExplainOfferDefaults.DedupKeySeparator + Guid.NewGuid().ToString(GuidFormat, CultureInfo.InvariantCulture);

    private ProactiveTriggerDispatchRow NewRow(string userId, string kind, string outcome, string dedupKey)
    {
        var reaction = ReactionOf(outcome);
        return new ProactiveTriggerDispatchRow
        {
            UserId = userId,
            TriggerKind = kind,
            DedupKey = dedupKey,
            ContentParamsJson = new ExplainOfferMarker { Outcome = outcome }.ToJson(),
            Severity = AgentTriggerSeverity.Low,
            Reaction = reaction,
            ReactionAtUtc = reaction == ProactiveReaction.None ? null : _timeProvider.GetUtcNow().UtcDateTime
        };
    }

    private static ProactiveReaction ReactionOf(string outcome) => outcome switch
    {
        UiExplainOfferOutcomes.Accepted => ProactiveReaction.Helpful,
        UiExplainOfferOutcomes.Shown => ProactiveReaction.None,
        _ => ProactiveReaction.Dismissed
    };
}
