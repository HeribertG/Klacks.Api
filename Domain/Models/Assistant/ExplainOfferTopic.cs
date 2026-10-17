// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// One topic Klacksy may offer to explain at a confirmation or preview gate before the user decides.
/// </summary>
/// <param name="Key">Short topic key; the trigger kind of its counter rows is ExplainOfferTopics.TriggerKindPrefix + Key</param>
/// <param name="HappenSkillName">Knowledge happen (memory key) whose curated content the explanation is grounded in</param>
/// <param name="Audience">Who may be offered the topic; advanced-only topics are never offered to beginners</param>
/// <param name="GateSkills">Skills whose held confirmation (a Confirmation result) is the gate of this topic</param>
/// <param name="PreviewGateSkills">Preview/apply skills whose apply=false preview result is a gate of this topic</param>
public sealed record ExplainOfferTopic(
    string Key,
    string HappenSkillName,
    ExplainOfferAudience Audience,
    IReadOnlyList<string> GateSkills,
    IReadOnlyList<string> PreviewGateSkills);
