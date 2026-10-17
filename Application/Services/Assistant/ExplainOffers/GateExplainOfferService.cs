// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IGateExplainOfferService"/>: the "explain briefly first" chip at the gates of consequential
/// actions. When a turn ends at a gate (GateOfferDetector) and the topic is allowed - setting on, proactive kill switch
/// off, audience fits, the user's per-kind preference lets it through, at most one offer per topic per day - the chip is
/// added to the turn's reply options (or, when the model offered none, to a yes/no pair) and a counter row
/// (agent_trigger_dispatches, kind "explain.topic.&lt;key&gt;", no ContentKey so it never reaches the inbox) remembers it under
/// the conversation together with the gate skill and the fingerprint of the held token. The chip is added to a COPY of the
/// model's options, returned only after the row is stored, so a failed write never leaves a chip without its counter row.
/// The user's next message settles it: the chip's own label accepts (Helpful, and the turn is grounded in the topic's
/// curated content by an ExplainOfferNote); anything else is Dismissed with an outcome in ContentParamsJson - proceeded
/// (an explicit yes to the gate), declined (a leading refusal), other, or expired (the offer waited longer than
/// ExplainOfferDefaults.PendingOfferMaxAgeMinutes, and then even the chip label no longer accepts). Only ignored offers
/// (other, expired) count towards the long snooze: three in a row snooze the topic for 30 days. The counter rows are
/// written directly, not through SetProactiveReactionCommandHandler, whose dismiss-streak evaluators would turn an ignored
/// offer into a mute suggestion. The explanation itself is one model turn grounded in the happen, not a canned reply: the
/// curated content is written for a model to translate into the user's language and depth, and a verbatim dump would print
/// its internal anchors. After an accepted offer exactly the held confirmation the offer was made at is renewed (matched by
/// token fingerprint, skill, purpose, channel and user), so the answer to the re-asked question still redeems it while no
/// other held confirmation is ever pulled back into the force window; a preview gate holds no token and renews nothing.
/// Every failure is logged and swallowed - an offer must never break a chat turn.
/// </summary>
/// <param name="dispatches">Counter rows: remembers the offer and settles it</param>
/// <param name="preferences">The user's per-kind mute/snooze; the long snooze after ignored offers is written here</param>
/// <param name="governance">Reads the proactive kill switch</param>
/// <param name="settingsReader">Reads the explain-offers setting</param>
/// <param name="knowledgeExecutor">Reads the curated happen content the explanation is grounded in</param>
/// <param name="confirmationRenewal">Gives the offered held confirmation a fresh lifetime after the detour turn</param>
/// <param name="timeProvider">Clock for the cooldown, the offer's maximum age, the reaction stamp and the snooze end</param>
/// <param name="logger">Reports a swallowed failure</param>

using System.Globalization;
using Klacks.Api.Application.Skills.Generic;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Security;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.ExplainOffers;

public sealed class GateExplainOfferService : IGateExplainOfferService
{
    private static readonly char[] TrailingPunctuation = ['.', '!', '?', '…'];

    private readonly IProactiveTriggerDispatchRepository _dispatches;
    private readonly IAgentTriggerPreferenceService _preferences;
    private readonly IProactiveGovernanceResolver _governance;
    private readonly ISettingsReader _settingsReader;
    private readonly KnowledgeHappenExecutor _knowledgeExecutor;
    private readonly IPendingConfirmationRenewal _confirmationRenewal;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GateExplainOfferService> _logger;

    public GateExplainOfferService(
        IProactiveTriggerDispatchRepository dispatches,
        IAgentTriggerPreferenceService preferences,
        IProactiveGovernanceResolver governance,
        ISettingsReader settingsReader,
        KnowledgeHappenExecutor knowledgeExecutor,
        IPendingConfirmationRenewal confirmationRenewal,
        TimeProvider timeProvider,
        ILogger<GateExplainOfferService> logger)
    {
        _dispatches = dispatches;
        _preferences = preferences;
        _governance = governance;
        _settingsReader = settingsReader;
        _knowledgeExecutor = knowledgeExecutor;
        _confirmationRenewal = confirmationRenewal;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task PrepareTurnAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(context.UserId) || string.IsNullOrWhiteSpace(context.ConversationId))
            {
                return;
            }

            var pending = await _dispatches.FindPendingExplainOfferAsync(
                context.UserId, context.ConversationId, cancellationToken);
            if (pending == null)
            {
                return;
            }

            var marker = ExplainOfferMarker.Parse(pending.ContentParamsJson);
            if (IsExpired(pending))
            {
                await SettleIgnoredOrAnsweredAsync(pending, marker, ExplainOfferOutcomes.Expired, context.UserId, cancellationToken);
                return;
            }

            var topic = ExplainOfferTopics.FindByTriggerKind(pending.TriggerKind);
            var note = topic != null && IsAcceptance(context.Message)
                ? await BuildAcceptedNoteAsync(topic, context, cancellationToken)
                : null;

            if (topic != null && note != null)
            {
                await SettleAsync(pending, marker, ProactiveReaction.Helpful, ExplainOfferOutcomes.Accepted, cancellationToken);
                context.ExplainOfferNote = note;
                context.ExplainOfferAcceptance = new ExplainOfferAcceptance(topic.Key, marker.GateSkill, marker.TokenFingerprint);
                return;
            }

            await SettleIgnoredOrAnsweredAsync(
                pending, marker, ClassifyAnswer(context.Message, context.Language), context.UserId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Settling the explain offer of user {UserId} failed", context.UserId);
        }
    }

    public async Task<SuggestedRepliesConfig?> AugmentRepliesAsync(
        LLMContext context,
        string? conversationId,
        IReadOnlyList<LLMFunctionCall>? functionCalls,
        SuggestedRepliesConfig? replies,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (context.ExplainOfferAcceptance != null)
            {
                return await AfterAcceptedOfferAsync(context, context.ExplainOfferAcceptance, replies, cancellationToken);
            }

            var gate = GateOfferDetector.Detect(functionCalls);
            if (gate == null || string.IsNullOrWhiteSpace(conversationId) || !IsOfferChannel(context))
            {
                return replies;
            }

            if (!await MayOfferAsync(context, gate.Topic, cancellationToken))
            {
                return replies;
            }

            var offered = WithChip(context, replies);
            if (offered == null)
            {
                return replies;
            }

            await RecordOfferAsync(context, gate, conversationId, cancellationToken);
            return offered;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Offering to explain at a gate failed for user {UserId}", context.UserId);
            return replies;
        }
    }

    private static bool IsOfferChannel(LLMContext context) =>
        Guid.TryParse(context.UserId, out _) && !context.IsVoiceMode && string.IsNullOrEmpty(context.CappedChannel);

    private bool IsExpired(ProactiveTriggerDispatchRow pending) =>
        pending.CreateTime is { } offeredAt
        && _timeProvider.GetUtcNow().UtcDateTime - offeredAt > TimeSpan.FromMinutes(ExplainOfferDefaults.PendingOfferMaxAgeMinutes);

    /// <summary>
    /// What a message that did not accept the offer did with the gate: an explicit yes proceeds (the same predicate that
    /// redeems a held token), a leading refusal declines, everything else - an unrelated message, a word the languages
    /// disagree about - is "other", i.e. the offer was ignored.
    /// </summary>
    private static string ClassifyAnswer(string? message, string? language)
    {
        if (AffirmationDetector.AssessExplicitConfirmation(message, language).IsConfirmed)
        {
            return ExplainOfferOutcomes.Proceeded;
        }

        return DeclineDetector.LeadsWithNegation(message, language)
            ? ExplainOfferOutcomes.Declined
            : ExplainOfferOutcomes.Other;
    }

    private async Task<bool> MayOfferAsync(LLMContext context, ExplainOfferTopic topic, CancellationToken cancellationToken)
    {
        if (topic.Audience == ExplainOfferAudience.AdvancedOnly
            && context.ExplainDepth?.Level != ExplainExperienceLevel.Advanced)
        {
            return false;
        }

        if (!await IsEnabledAsync() || await _governance.IsKillSwitchActiveAsync(cancellationToken))
        {
            return false;
        }

        var kind = ExplainOfferTopics.TriggerKindOf(topic);
        if (!await _preferences.IsAllowedAsync(context.UserId, kind, AgentTriggerSeverity.Low))
        {
            return false;
        }

        var cooldownStart = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-ExplainOfferDefaults.OfferCooldownHours);
        return !await _dispatches.HasRecentAsync(context.UserId, kind, cooldownStart, cancellationToken);
    }

    private async Task<bool> IsEnabledAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.KlacksyExplainOffersEnabled);
        return string.IsNullOrWhiteSpace(setting?.Value) || !bool.TryParse(setting.Value, out var enabled) || enabled;
    }

    /// <summary>
    /// A copy of the reply options with the chip appended, null when no chip may be added. The model's own object is never
    /// touched: the caller hands it back unchanged whenever the offer is not recorded.
    /// </summary>
    private static SuggestedRepliesConfig? WithChip(LLMContext context, SuggestedRepliesConfig? replies)
    {
        if (!ExplainOfferTexts.TryGetText(ExplainOfferTexts.ChipLabel, context.Language, out var label))
        {
            return null;
        }

        var source = replies ?? YesNoReplies(context.Language);
        if (source == null
            || !string.Equals(source.SelectionMode, SuggestedReplySelectionModes.Single, StringComparison.OrdinalIgnoreCase)
            || source.Options.Count >= LlmRepliesFormat.MaxOptions
            || source.Options.Any(option => IsSame(option.Label, label) || IsSame(option.Value, label)))
        {
            return null;
        }

        var copy = CopyOf(source);
        copy.Options.Add(new SuggestedReply { Label = label, Value = label });
        return copy;
    }

    private static SuggestedRepliesConfig CopyOf(SuggestedRepliesConfig source) => new()
    {
        SelectionMode = source.SelectionMode,
        Prompt = source.Prompt,
        Min = source.Min,
        Max = source.Max,
        Step = source.Step,
        Options = source.Options
            .Select(option => new SuggestedReply { Label = option.Label, Value = option.Value })
            .ToList()
    };

    private async Task<SuggestedRepliesConfig?> AfterAcceptedOfferAsync(
        LLMContext context, ExplainOfferAcceptance acceptance, SuggestedRepliesConfig? replies, CancellationToken cancellationToken)
    {
        await RenewOfferedConfirmationAsync(context, acceptance, cancellationToken);
        return replies is { Options.Count: > 0 } ? replies : YesNoReplies(context.Language) ?? replies;
    }

    private static SuggestedRepliesConfig? YesNoReplies(string? language)
    {
        if (!GracefulCorrectionTexts.TryGetText(GracefulCorrectionTexts.RecipeConfirmYes, language, out var yes)
            || !GracefulCorrectionTexts.TryGetText(GracefulCorrectionTexts.RecipeConfirmNo, language, out var no))
        {
            return null;
        }

        return new SuggestedRepliesConfig
        {
            SelectionMode = SuggestedReplySelectionModes.Single,
            Options =
            [
                new SuggestedReply { Label = yes, Value = yes },
                new SuggestedReply { Label = no, Value = no }
            ]
        };
    }

    /// <summary>
    /// Renews the one confirmation the accepted offer was made at, and only when everything the offer recorded points at a
    /// held gate of the accepted topic: a fingerprint (a preview gate has none), a gate skill of that topic, and a purpose
    /// the topic may renew. The store matches the row by fingerprint, skill, purpose, channel and user, so an unrelated
    /// confirmation of the same user is never touched.
    /// </summary>
    private async Task RenewOfferedConfirmationAsync(
        LLMContext context, ExplainOfferAcceptance acceptance, CancellationToken cancellationToken)
    {
        var topic = ExplainOfferTopics.All.FirstOrDefault(
            candidate => string.Equals(candidate.Key, acceptance.TopicKey, StringComparison.Ordinal));
        if (topic == null
            || string.IsNullOrEmpty(acceptance.TokenFingerprint)
            || string.IsNullOrEmpty(acceptance.GateSkillName)
            || !topic.GateSkills.Contains(acceptance.GateSkillName, StringComparer.OrdinalIgnoreCase)
            || !Guid.TryParse(context.UserId, out var userId))
        {
            return;
        }

        var purposes = ExplainOfferTopics.RenewablePurposesOf(topic);
        if (purposes.Count == 0)
        {
            return;
        }

        await _confirmationRenewal.RenewOfferedAsync(
            userId, acceptance.TokenFingerprint, acceptance.GateSkillName, purposes,
            PendingConfirmationChannels.Chat, cancellationToken);
    }

    private async Task RecordOfferAsync(
        LLMContext context, GateOffer gate, string conversationId, CancellationToken cancellationToken)
    {
        var marker = new ExplainOfferMarker
        {
            Level = (context.ExplainDepth?.Level ?? ExplainExperienceLevel.Beginner).ToString().ToLowerInvariant(),
            GateSkill = gate.GateSkillName,
            TokenFingerprint = string.IsNullOrEmpty(gate.ConfirmationToken)
                ? null
                : ConfirmationTokenHasher.Hash(gate.ConfirmationToken)
        };
        var turn = (context.TurnId ?? Guid.NewGuid()).ToString("N", CultureInfo.InvariantCulture);

        await _dispatches.RecordAsync(
            new ProactiveTriggerDispatchRow
            {
                UserId = context.UserId,
                TriggerKind = ExplainOfferTopics.TriggerKindOf(gate.Topic),
                DedupKey = conversationId + ExplainOfferDefaults.DedupKeySeparator + turn,
                ContentParamsJson = marker.ToJson(),
                Severity = AgentTriggerSeverity.Low
            },
            cancellationToken);
    }

    private async Task SettleIgnoredOrAnsweredAsync(
        ProactiveTriggerDispatchRow pending,
        ExplainOfferMarker marker,
        string outcome,
        string userId,
        CancellationToken cancellationToken)
    {
        await SettleAsync(pending, marker, ProactiveReaction.Dismissed, outcome, cancellationToken);
        if (ExplainOfferOutcomes.IsIgnored(outcome))
        {
            await SnoozeAfterIgnoredStreakAsync(userId, pending.TriggerKind, cancellationToken);
        }
    }

    private async Task SettleAsync(
        ProactiveTriggerDispatchRow row,
        ExplainOfferMarker marker,
        ProactiveReaction reaction,
        string outcome,
        CancellationToken cancellationToken)
    {
        marker.Outcome = outcome;
        row.ContentParamsJson = marker.ToJson();
        row.Reaction = reaction;
        row.ReactionAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _dispatches.UpdateAsync(row, cancellationToken);
    }

    private async Task SnoozeAfterIgnoredStreakAsync(string userId, string triggerKind, CancellationToken cancellationToken)
    {
        var recent = await _dispatches.GetRecentReactionsAsync(
            userId, triggerKind, ExplainOfferDefaults.IgnoredOffersBeforeLongSnooze, cancellationToken);
        if (recent.Count < ExplainOfferDefaults.IgnoredOffersBeforeLongSnooze
            || recent.Any(row => row.Reaction != ProactiveReaction.Dismissed
                || !ExplainOfferOutcomes.IsIgnored(ExplainOfferMarker.Parse(row.ContentParamsJson).Outcome)))
        {
            return;
        }

        await _preferences.SnoozeAsync(
            userId, triggerKind, _timeProvider.GetUtcNow().UtcDateTime.AddDays(ExplainOfferDefaults.LongSnoozeDays));
    }

    private async Task<string?> BuildAcceptedNoteAsync(
        ExplainOfferTopic topic, LLMContext context, CancellationToken cancellationToken)
    {
        var content = await _knowledgeExecutor.ReadKnowledgeAsync(
            topic.HappenSkillName, ExplainOfferDefaults.ExplanationLevel, context.ExplainDepth, cancellationToken);
        return content == null
            ? null
            : string.Format(
                CultureInfo.InvariantCulture, ExplainOfferDefaults.AcceptedNoteTemplate,
                KnowledgeHappenExecutor.AnswerInstruction, content);
    }

    private static bool IsAcceptance(string? message)
    {
        var normalized = Normalize(message);
        return normalized.Length > 0
            && ExplainOfferTexts.AllTextsOf(ExplainOfferTexts.ChipLabel).Any(text => IsSame(text, normalized));
    }

    private static bool IsSame(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? text) => (text ?? string.Empty).Trim().TrimEnd(TrailingPunctuation).Trim();
}
