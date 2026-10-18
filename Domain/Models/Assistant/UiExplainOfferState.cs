// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Whether the UI may show an explain offer of one topic to the calling user right now.
/// </summary>
/// <param name="Enabled">Explain offers are switched on for the installation (setting on, proactive kill switch off)</param>
/// <param name="ShouldOffer">The offer may be shown to this user: enabled, the user's level is offered, and the topic is not muted for the user</param>
public sealed record UiExplainOfferState(bool Enabled, bool ShouldOffer)
{
    public static readonly UiExplainOfferState Off = new(false, false);
}
