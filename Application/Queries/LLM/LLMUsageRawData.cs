namespace Klacks.Api.Application.Queries.LLM;

public class LLMUsageRawData
{
    public List<Domain.Models.LLM.LLMUsage> Usages { get; set; } = new();

    public Dictionary<string, decimal> ModelSummary { get; set; } = new();

    public decimal TotalCost { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}