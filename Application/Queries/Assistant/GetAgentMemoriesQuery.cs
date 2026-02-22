using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetAgentMemoriesQuery : IRequest<object>
{
    public Guid AgentId { get; set; }
    public string? Search { get; set; }
    public string? Category { get; set; }
}
