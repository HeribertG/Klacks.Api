// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles the ListSealedOrdersQuery by delegating to ISealedOrderListLoader.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Exports;

public class ListSealedOrdersQueryHandler : IRequestHandler<ListSealedOrdersQuery, List<SealedOrderListItem>>
{
    private readonly ISealedOrderListLoader _loader;
    private readonly ILogger<ListSealedOrdersQueryHandler> _logger;

    public ListSealedOrdersQueryHandler(
        ISealedOrderListLoader loader,
        ILogger<ListSealedOrdersQueryHandler> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async Task<List<SealedOrderListItem>> Handle(ListSealedOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Loading sealed orders list (from={From}, until={Until}, customer={Customer})",
            request.FromDate, request.UntilDate, request.CustomerId);

        return await _loader.LoadAsync(
            request.FromDate, request.UntilDate, request.CustomerId, cancellationToken);
    }
}
