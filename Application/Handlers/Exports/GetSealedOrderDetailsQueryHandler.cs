// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles the GetSealedOrderDetailsQuery by delegating to ISealedOrderDetailsLoader.
/// @param request - Contains the sealed order id and the optional date range
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Exports;

public class GetSealedOrderDetailsQueryHandler : IRequestHandler<GetSealedOrderDetailsQuery, SealedOrderDetailsResource?>
{
    private readonly ISealedOrderDetailsLoader _loader;
    private readonly ILogger<GetSealedOrderDetailsQueryHandler> _logger;

    public GetSealedOrderDetailsQueryHandler(
        ISealedOrderDetailsLoader loader,
        ILogger<GetSealedOrderDetailsQueryHandler> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async Task<SealedOrderDetailsResource?> Handle(GetSealedOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Loading sealed order details (id={Id}, from={From}, until={Until})",
            request.Id, request.FromDate, request.UntilDate);

        return await _loader.LoadAsync(request.Id, request.FromDate, request.UntilDate, cancellationToken);
    }
}
