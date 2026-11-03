// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Shifts;

/// <summary>
/// Returns the date range in which the cuts of a sealed order may be reset. A sealed order a group-restricted caller
/// may not see is answered exactly like a missing one. A sealed order inside a visible variant (key) is found through the
/// token stored on it (the request carries none), also while a scenario carries the key; a scenario clone and a hidden
/// variant are answered like a missing order, and so is an order with a row the caller may not see. The earliest date is the
/// start of the order, or with real work the day after the last one (ShiftResetRules).
/// </summary>
/// <param name="shiftRepository">Loads the sealed order with its group items</param>
/// <param name="shiftVisibilityGuard">Decides whether the caller may see the order</param>
/// <param name="variantShiftWriteGuard">Decides whether the token stored on the order belongs to a readable variant</param>
/// <param name="orderFamilyEndRepository">Finds the last real work of the family</param>
public class GetResetDateRangeQueryHandler : IRequestHandler<GetResetDateRangeQuery, ResetDateRangeResponse>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IVariantShiftWriteGuard _variantShiftWriteGuard;
    private readonly IOrderFamilyEndRepository _orderFamilyEndRepository;
    private readonly ILogger<GetResetDateRangeQueryHandler> _logger;

    public GetResetDateRangeQueryHandler(
        IShiftRepository shiftRepository,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IVariantShiftWriteGuard variantShiftWriteGuard,
        IOrderFamilyEndRepository orderFamilyEndRepository,
        ILogger<GetResetDateRangeQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _variantShiftWriteGuard = variantShiftWriteGuard;
        _orderFamilyEndRepository = orderFamilyEndRepository;
        _logger = logger;
    }

    public async Task<ResetDateRangeResponse> Handle(
        GetResetDateRangeQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting reset date range for OriginalId: {OriginalId}", request.OriginalId);

        var sealedOrder = await _shiftRepository.GetSealedOrder(request.OriginalId)
            ?? await VariantSealedOrderLookup.FindAsync(
                _shiftRepository, _variantShiftWriteGuard, request.OriginalId, cancellationToken);

        if (sealedOrder == null || !await IsWholeFamilyVisibleAsync(sealedOrder, cancellationToken))
        {
            _logger.LogWarning("SealedOrder with OriginalId {OriginalId} not found", request.OriginalId);
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.SealedOrderNotFound, request.OriginalId));
        }

        var lastWork = await _orderFamilyEndRepository.FindLastWorkDateAfterAsync(sealedOrder.Id, DateOnly.MinValue, cancellationToken);

        var response = new ResetDateRangeResponse
        {
            EarliestResetDate = ShiftResetRules.EarliestAllowed(sealedOrder.FromDate, lastWork),
            UntilDate = sealedOrder.UntilDate
        };

        _logger.LogInformation(
            "Reset date range for OriginalId {OriginalId}: EarliestResetDate={EarliestResetDate}, UntilDate={UntilDate}",
            request.OriginalId,
            response.EarliestResetDate,
            response.UntilDate);

        return response;
    }

    private async Task<bool> IsWholeFamilyVisibleAsync(Shift sealedOrder, CancellationToken cancellationToken)
    {
        if (!await _shiftVisibilityGuard.IsVisibleAsync(sealedOrder, cancellationToken))
        {
            return false;
        }

        var rows = await _shiftRepository.CutList(sealedOrder.Id, analyseToken: sealedOrder.AnalyseToken);
        return await _shiftVisibilityGuard.FindFirstHiddenAsync(rows.Select(row => row.Id).ToList(), cancellationToken) == null;
    }
}
