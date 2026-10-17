// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// The explain offer the user accepted this turn, carried from the turn's preparation to its end, where exactly the
/// confirmation the offer was made at is renewed.
/// </summary>
/// <param name="TopicKey">Key of the accepted topic (ExplainOfferTopics)</param>
/// <param name="GateSkillName">The skill whose gate the offer was made at; null when the offer row does not name it</param>
/// <param name="TokenFingerprint">ConfirmationTokenHasher fingerprint of the held token; null for a preview gate</param>
public sealed record ExplainOfferAcceptance(string TopicKey, string? GateSkillName, string? TokenFingerprint);
