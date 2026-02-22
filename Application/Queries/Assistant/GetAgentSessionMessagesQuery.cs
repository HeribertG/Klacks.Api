using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetAgentSessionMessagesQuery : IRequest<object>
{
    public Guid AgentId { get; set; }
    public Guid SessionId { get; set; }
}
