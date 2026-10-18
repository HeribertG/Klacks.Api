// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answers whether the UI may show the explain offer of one topic to the caller; null for an unknown topic.
/// </summary>
/// <param name="offers">Reads the offer state of the caller</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetUiExplainOfferStateQueryHandler : IRequestHandler<GetUiExplainOfferStateQuery, UiExplainOfferStateResource?>
{
    private readonly IUiExplainOfferService _offers;

    public GetUiExplainOfferStateQueryHandler(IUiExplainOfferService offers)
    {
        _offers = offers;
    }

    public async Task<UiExplainOfferStateResource?> Handle(GetUiExplainOfferStateQuery request, CancellationToken cancellationToken)
    {
        var topic = UiExplainOfferTopics.Find(request.TopicKey);
        if (topic == null)
        {
            return null;
        }

        var state = await _offers.GetStateAsync(request.UserId, topic.Key, cancellationToken);
        return new UiExplainOfferStateResource
        {
            TopicKey = topic.Key,
            ShouldOffer = state.ShouldOffer
        };
    }
}
