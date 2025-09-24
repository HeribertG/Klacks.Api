using MediatR;

namespace Klacks.Api.Application.Queries.LLM;

public class GetLLMUsageQuery : IRequest<LLMUsageResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int Days { get; set; } = 30;
}