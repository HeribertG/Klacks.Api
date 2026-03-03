// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Queries.Assistant;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetLLMUsageQueryHandler : IRequestHandler<GetLLMUsageQuery, LLMUsageResponse>
{
    private readonly ILLMRepository _repository;
    private readonly LLMMapper _lLMMapper;

    public GetLLMUsageQueryHandler(ILLMRepository repository, LLMMapper lLMMapper)
    {
        _repository = repository;
        _lLMMapper = lLMMapper;
    }

    public async Task<LLMUsageResponse> Handle(GetLLMUsageQuery request, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddDays(-request.Days);
        var endDate = DateTime.UtcNow;

        var usages = await _repository.GetUserUsageAsync(request.UserId, startDate, endDate);
        var modelSummary = await _repository.GetUsageSummaryByModelAsync(request.UserId, request.Days);
        var totalCost = await _repository.GetTotalCostAsync(request.UserId, request.Days);

        var rawData = new LLMUsageRawData
        {
            Usages = usages,
            ModelSummary = modelSummary,
            TotalCost = totalCost,
            StartDate = startDate,
            EndDate = endDate
        };

        return _lLMMapper.ToUsageResponse(rawData);
    }
}