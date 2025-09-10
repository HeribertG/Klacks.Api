using MediatR;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Queries.LLM;

public class GetLLMUsageQuery : IRequest<LLMUsageResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int Days { get; set; } = 30;
}

public class LLMUsageResponse
{
    public int TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Dictionary<string, decimal> ModelUsage { get; set; } = new();
    public List<DailyUsage> DailyUsage { get; set; } = new();
}

public class DailyUsage
{
    public DateTime Date { get; set; }
    public int Tokens { get; set; }
    public decimal Cost { get; set; }
    public int Requests { get; set; }
}

public class GetLLMUsageQueryHandler : IRequestHandler<GetLLMUsageQuery, LLMUsageResponse>
{
    private readonly ILLMRepository _repository;

    public GetLLMUsageQueryHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<LLMUsageResponse> Handle(GetLLMUsageQuery request, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddDays(-request.Days);
        var endDate = DateTime.UtcNow;

        var usages = await _repository.GetUserUsageAsync(request.UserId, startDate, endDate);
        var modelSummary = await _repository.GetUsageSummaryByModelAsync(request.UserId, request.Days);
        var totalCost = await _repository.GetTotalCostAsync(request.UserId, request.Days);

        var dailyUsage = usages
            .GroupBy(u => u.CreateTime?.Date ?? DateTime.UtcNow.Date)
            .Select(g => new DailyUsage
            {
                Date = g.Key,
                Tokens = g.Sum(u => u.TotalTokens),
                Cost = g.Sum(u => u.Cost),
                Requests = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new LLMUsageResponse
        {
            TotalTokens = usages.Sum(u => u.TotalTokens),
            TotalCost = totalCost,
            StartDate = startDate,
            EndDate = endDate,
            ModelUsage = modelSummary,
            DailyUsage = dailyUsage
        };
    }
}