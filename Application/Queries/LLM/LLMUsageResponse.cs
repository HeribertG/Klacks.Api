namespace Klacks.Api.Application.Queries.LLM;

public class LLMUsageResponse
{
    public int TotalTokens { get; set; }

    public decimal TotalCost { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Dictionary<string, decimal> ModelUsage { get; set; } = new();

    public List<DailyUsage> DailyUsage { get; set; } = new();
}