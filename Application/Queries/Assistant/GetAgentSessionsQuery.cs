using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetAgentSessionsQuery : IRequest<object>
{
    public Guid AgentId { get; set; }
    public string UserId { get; set; } = string.Empty;
}
