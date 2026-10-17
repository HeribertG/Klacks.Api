// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Shifts;

/// <summary>
/// Returns the date range in which the cuts of a sealed order may be reset. A sealed order a group-restricted caller
/// may not see is answered exactly like a missing one.
/// </summary>
/// <param name="shiftRepository">Loads the sealed order with its group items</param>
/// <param name="shiftVisibilityGuard">Decides whether the caller may see the order</param>
public class GetResetDateRangeQueryHandler : IRequestHandler<GetResetDateRangeQuery, ResetDateRangeResponse>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly ILogger<GetResetDateRangeQueryHandler> _logger;

    public GetResetDateRangeQueryHandler(
        IShiftRepository shiftRepository,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<GetResetDateRangeQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _logger = logger;
    }

    public async Task<ResetDateRangeResponse> Handle(
        GetResetDateRangeQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting reset date range for OriginalId: {OriginalId}", request.OriginalId);

        var sealedOrder = await _shiftRepository.GetSealedOrder(request.OriginalId);

        if (sealedOrder == null || !await _shiftVisibilityGuard.IsVisibleAsync(sealedOrder, cancellationToken))
        {
            _logger.LogWarning("SealedOrder with OriginalId {OriginalId} not found", request.OriginalId);
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.SealedOrderNotFound, request.OriginalId));
        }

        var response = new ResetDateRangeResponse
        {
            EarliestResetDate = sealedOrder.FromDate,
            UntilDate = sealedOrder.UntilDate
        };

        _logger.LogInformation(
            "Reset date range for OriginalId {OriginalId}: EarliestResetDate={EarliestResetDate}, UntilDate={UntilDate}",
            request.OriginalId,
            response.EarliestResetDate,
            response.UntilDate);

        return response;
    }
}
