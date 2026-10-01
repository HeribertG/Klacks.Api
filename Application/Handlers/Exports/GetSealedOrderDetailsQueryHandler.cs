// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles the GetSealedOrderDetailsQuery by delegating to ISealedOrderDetailsLoader.
/// Work entries of employees hidden from the caller by group visibility are left out of the preview.
/// @param request - Contains the sealed order id and the optional date range
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Exports;

public class GetSealedOrderDetailsQueryHandler : IRequestHandler<GetSealedOrderDetailsQuery, SealedOrderDetailsResource?>
{
    private readonly ISealedOrderDetailsLoader _loader;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ILogger<GetSealedOrderDetailsQueryHandler> _logger;

    public GetSealedOrderDetailsQueryHandler(
        ISealedOrderDetailsLoader loader,
        IClientVisibilityGuard clientVisibilityGuard,
        ILogger<GetSealedOrderDetailsQueryHandler> logger)
    {
        _loader = loader;
        _clientVisibilityGuard = clientVisibilityGuard;
        _logger = logger;
    }

    public async Task<SealedOrderDetailsResource?> Handle(GetSealedOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Loading sealed order details (id={Id}, from={From}, until={Until})",
            request.Id, request.FromDate, request.UntilDate);

        var details = await _loader.LoadAsync(request.Id, request.FromDate, request.UntilDate, cancellationToken);
        if (details == null)
        {
            return null;
        }

        var employeeIds = details.WorkEntries.Select(w => w.EmployeeId).Distinct().ToList();
        var visibleIds = (await _clientVisibilityGuard.FilterVisibleAsync(employeeIds, id => id, cancellationToken)).ToHashSet();
        details.WorkEntries = details.WorkEntries.Where(w => visibleIds.Contains(w.EmployeeId)).ToList();

        return details;
    }
}
