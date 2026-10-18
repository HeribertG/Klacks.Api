// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// One topic the UI may offer Klacksy's short explanation for, when the user does something whose meaning is known to
/// the UI (ticking FT or +FT on a shift).
/// </summary>
/// <param name="Key">Short topic key; the trigger kind of its counter rows is UiExplainOfferTopics.TriggerKindPrefix + Key</param>
/// <param name="HappenSkillName">Knowledge happen the UI's trigger phrase is guaranteed to reach</param>
public sealed record UiExplainOfferTopic(string Key, string HappenSkillName);
