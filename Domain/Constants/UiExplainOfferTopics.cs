// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The topics the UI offers Klacksy's short explanation for (T5): a UI signal whose meaning the UI knows, such as the
/// first tick of FT or +FT on a shift. The counter rows (agent_trigger_dispatches) of a topic carry the trigger kind
/// TriggerKindPrefix + Key, which is also the key of the user's per-kind mute. The prefix deliberately differs from
/// ExplainOfferTopics.TriggerKindPrefix ("explain.topic."): the gate offers of T4 read every row below their own prefix
/// as a pending chip of the conversation, so a shared prefix would let the T4 service settle UI offers.
/// WorkBookingRefusalKey is passive (T5 part b): the "Why? Ask Klacksy" link of a refused single booking never asks
/// for the offer state and records only "accepted" on click, at most one row per user and company-local day, and never
/// mutes the kind, so the counter shows on how many days each account used the link.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Constants;

public static class UiExplainOfferTopics
{
    public const string TriggerKindPrefix = "explain.ui.";

    public const string ShiftHolidayOptionsKey = "shift-holiday-options";

    public const string WorkBookingRefusalKey = "work-booking-refusal";

    public static readonly IReadOnlyList<UiExplainOfferTopic> All =
    [
        new(ShiftHolidayOptionsKey, SkillNames.ExplainShiftHolidayOptions),
        new(WorkBookingRefusalKey, SkillNames.DiagnoseWorkBooking, IsPassive: true)
    ];

    /// <summary>The topic of a key, null when the key is no (longer) a known topic.</summary>
    /// <param name="topicKey">Topic key as sent by the UI</param>
    public static UiExplainOfferTopic? Find(string? topicKey) =>
        All.FirstOrDefault(topic => string.Equals(topic.Key, topicKey, StringComparison.Ordinal));

    /// <summary>The trigger kind of the counter rows and of the mute of one topic.</summary>
    /// <param name="topic">The topic whose kind is wanted</param>
    public static string TriggerKindOf(UiExplainOfferTopic topic) => TriggerKindPrefix + topic.Key;

    /// <summary>True when the trigger kind belongs to a UI explain offer rather than to a detector notification.</summary>
    /// <param name="triggerKind">Trigger kind of a dispatch row</param>
    public static bool IsOfferKind(string? triggerKind) =>
        triggerKind != null && triggerKind.StartsWith(TriggerKindPrefix, StringComparison.Ordinal);
}
