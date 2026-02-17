namespace Klacks.Api.Application.DTOs.Assistant;

public class LLMUsageInfo
{
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int TotalTokens => InputTokens + OutputTokens;

    public decimal Cost { get; set; }
}