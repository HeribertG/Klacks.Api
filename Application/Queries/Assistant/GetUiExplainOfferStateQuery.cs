// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

/// <summary>
/// Reads whether the UI may show the explain offer of one topic to the caller. Null result = unknown topic.
/// </summary>
/// <param name="UserId">Caller's account id, populated by the controller from the token</param>
/// <param name="TopicKey">Topic key (UiExplainOfferTopics)</param>
public class GetUiExplainOfferStateQuery : IRequest<UiExplainOfferStateResource?>
{
    public string UserId { get; set; } = string.Empty;

    public string TopicKey { get; set; } = string.Empty;
}
