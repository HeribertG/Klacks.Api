namespace Klacks.Api.Application.DTOs.LLM;

public class GetConversationHistoryRequest
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
    public string? UserId { get; set; }
}