// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <summary>
/// Records the caller's reaction to a UI explain offer. Result false = unknown topic or outcome, nothing written.
/// </summary>
/// <param name="UserId">Caller's account id, populated by the controller from the token</param>
/// <param name="TopicKey">Topic key (UiExplainOfferTopics)</param>
/// <param name="Outcome">shown, accepted, later or declined (UiExplainOfferOutcomes)</param>
public class RecordUiExplainOfferReactionCommand : IRequest<bool>
{
    public string UserId { get; set; } = string.Empty;

    public string TopicKey { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;
}
