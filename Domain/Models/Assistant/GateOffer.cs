// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// The gate a turn ended at, as GateOfferDetector found it.
/// </summary>
/// <param name="Topic">The explain-offer topic of the gate</param>
/// <param name="GateSkillName">The skill whose call is the gate</param>
/// <param name="ConfirmationToken">The token the held call issued; null for a preview gate, which holds nothing</param>
public sealed record GateOffer(ExplainOfferTopic Topic, string GateSkillName, string? ConfirmationToken);
