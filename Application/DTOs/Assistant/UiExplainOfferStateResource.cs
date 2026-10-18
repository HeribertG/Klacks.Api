// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Whether the UI may show the explain offer of one topic to the signed-in user.
/// </summary>
public class UiExplainOfferStateResource
{
    public string TopicKey { get; set; } = string.Empty;

    public bool ShouldOffer { get; set; }
}
