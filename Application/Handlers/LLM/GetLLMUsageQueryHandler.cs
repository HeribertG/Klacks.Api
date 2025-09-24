using MediatR;
using AutoMapper;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Queries.LLM;

namespace Klacks.Api.Application.Handlers.LLM;

public class GetLLMUsageQueryHandler : IRequestHandler<GetLLMUsageQuery, LLMUsageResponse>
{
    private readonly ILLMRepository _repository;
    private readonly IMapper _mapper;

    public GetLLMUsageQueryHandler(ILLMRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
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

        return _mapper.Map<LLMUsageResponse>(rawData);
    }
}