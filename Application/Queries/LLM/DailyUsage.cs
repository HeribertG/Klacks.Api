namespace Klacks.Api.Application.Queries.LLM;

public class DailyUsage
{
    public DateTime Date { get; set; }

    public int Tokens { get; set; }

    public decimal Cost { get; set; }

    public int Requests { get; set; }
}