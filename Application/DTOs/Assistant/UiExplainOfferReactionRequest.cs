// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Body of a UI explain offer reaction: one of shown, accepted, later, declined.
/// </summary>
public class UiExplainOfferReactionRequest
{
    public string Outcome { get; set; } = string.Empty;
}
