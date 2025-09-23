namespace Klacks.Api.Domain.Services.LLM.Providers;

public class LLMUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public decimal Cost { get; set; }
}