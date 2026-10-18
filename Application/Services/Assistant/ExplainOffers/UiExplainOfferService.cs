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
/// rejected notification. Reading is fail-closed: any failure means "do not offer". Logs never carry the user.
/// </summary>
/// <param name="dispatches">Counter rows of the offers</param>
/// <param name="preferences">The user's per-kind mute; the dosage is written here</param>
/// <param name="governance">Reads the proactive kill switch</param>
/// <param name="settingsReader">Reads the explain-offers setting</param>
/// <param name="explainDepthResolver">Resolves the user's explanation level (beginner/advanced)</param>
/// <param name="timeProvider">Clock for the reaction stamp</param>
/// <param name="logger">Reports a swallowed failure</param>

using System.Globalization;
using System.Text.Json;
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
    private readonly IProactiveGovernanceResolver _governance;
    private readonly ISettingsReader _settingsReader;
    private readonly IExplainDepthResolver _explainDepthResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UiExplainOfferService> _logger;

    public UiExplainOfferService(
        IProactiveTriggerDispatchRepository dispatches,
        IAgentTriggerPreferenceService preferences,
        IProactiveGovernanceResolver governance,
        ISettingsReader settingsReader,
        IExplainDepthResolver explainDepthResolver,
        TimeProvider timeProvider,
        ILogger<UiExplainOfferService> logger)
    {
        _dispatches = dispatches;
        _preferences = preferences;
        _governance = governance;
        _settingsReader = settingsReader;
        _explainDepthResolver = explainDepthResolver;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<UiExplainOfferState> GetStateAsync(
        string userId, string topicKey, CancellationToken cancellationToken = default)
    {
        var topic = UiExplainOfferTopics.Find(topicKey);
        if (topic == null || string.IsNullOrWhiteSpace(userId))
        {
            return UiExplainOfferState.Off;
        }

        try
        {
            if (!await IsEnabledAsync() || await _governance.IsKillSwitchActiveAsync(cancellationToken))
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
        await _dispatches.RecordAsync(NewRow(userId, topic, kind, outcome), cancellationToken);

        if (await ShouldMuteAfterAsync(userId, kind, outcome, cancellationToken))
        {
            await _preferences.MuteAsync(userId, kind, true);
        }

        return true;
    }

    private async Task<bool> IsEnabledAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.KlacksyExplainOffersEnabled);
        return string.IsNullOrWhiteSpace(setting?.Value) || !bool.TryParse(setting.Value, out var enabled) || enabled;
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
                return recent.Count(row => OutcomeOf(row) == UiExplainOfferOutcomes.Later)
                    >= UiExplainOfferDefaults.MaxLaterBeforeMute;
            default:
                return await _dispatches.CountUnreactedAsync(userId, kind, cancellationToken)
                    >= UiExplainOfferDefaults.MaxShownBeforeMute;
        }
    }

    private ProactiveTriggerDispatchRow NewRow(string userId, UiExplainOfferTopic topic, string kind, string outcome)
    {
        var reaction = ReactionOf(outcome);
        return new ProactiveTriggerDispatchRow
        {
            UserId = userId,
            TriggerKind = kind,
            DedupKey = topic.Key + UiExplainOfferDefaults.DedupKeySeparator + outcome
                + UiExplainOfferDefaults.DedupKeySeparator + Guid.NewGuid().ToString(GuidFormat, CultureInfo.InvariantCulture),
            ContentParamsJson = JsonSerializer.Serialize(
                new Dictionary<string, string> { [UiExplainOfferDefaults.OutcomeProperty] = outcome }),
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

    private static string? OutcomeOf(ProactiveTriggerDispatchRow row)
    {
        if (string.IsNullOrWhiteSpace(row.ContentParamsJson))
        {
            return null;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(row.ContentParamsJson);
            return values != null && values.TryGetValue(UiExplainOfferDefaults.OutcomeProperty, out var value) ? value : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
