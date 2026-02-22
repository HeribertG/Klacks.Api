using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetSoulSectionsQuery : IRequest<object>
{
    public Guid AgentId { get; set; }
}
