using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetResetDateRangeQueryHandler : IRequestHandler<GetResetDateRangeQuery, ResetDateRangeResponse>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<GetResetDateRangeQueryHandler> _logger;

    public GetResetDateRangeQueryHandler(
        IShiftRepository shiftRepository,
        ILogger<GetResetDateRangeQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public async Task<ResetDateRangeResponse> Handle(
        GetResetDateRangeQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting reset date range for OriginalId: {OriginalId}", request.OriginalId);

        var sealedOrder = await _shiftRepository.GetSealedOrder(request.OriginalId);

        if (sealedOrder == null)
        {
            _logger.LogWarning("SealedOrder with OriginalId {OriginalId} not found", request.OriginalId);
            throw new KeyNotFoundException($"SealedOrder with OriginalId {request.OriginalId} not found");
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
