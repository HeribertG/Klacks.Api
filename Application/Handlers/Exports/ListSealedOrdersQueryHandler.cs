// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles the ListSealedOrdersQuery by delegating to ISealedOrderListLoader. Orders of groups hidden from the caller are
/// left out, with the same shift group rule as the shift and order lists (admins and callers without a user are unrestricted).
/// </summary>
/// <param name="loader">Loads the sealed orders, restricted to the given visible groups</param>
/// <param name="shiftGroupFilterService">Resolves the caller's shift group restriction</param>
/// <param name="logger">Logs the requested filters</param>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Exports;

public class ListSealedOrdersQueryHandler : IRequestHandler<ListSealedOrdersQuery, List<SealedOrderListItem>>
{
    private readonly ISealedOrderListLoader _loader;
    private readonly IShiftGroupFilterService _shiftGroupFilterService;
    private readonly ILogger<ListSealedOrdersQueryHandler> _logger;

    public ListSealedOrdersQueryHandler(
        ISealedOrderListLoader loader,
        IShiftGroupFilterService shiftGroupFilterService,
        ILogger<ListSealedOrdersQueryHandler> logger)
    {
        _loader = loader;
        _shiftGroupFilterService = shiftGroupFilterService;
        _logger = logger;
    }

    public async Task<List<SealedOrderListItem>> Handle(ListSealedOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Loading sealed orders list (from={From}, until={Until}, customer={Customer}, search={Search})",
            request.FromDate, request.UntilDate, request.CustomerId, request.SearchTerm);

        var visibleGroupIds = await _shiftGroupFilterService.ResolveShiftListGroupIdsAsync(null);

        return await _loader.LoadAsync(
            request.FromDate, request.UntilDate, request.CustomerId, request.SearchTerm, visibleGroupIds, cancellationToken);
    }
}
