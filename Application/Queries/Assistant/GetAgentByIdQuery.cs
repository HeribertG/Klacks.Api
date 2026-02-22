using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetAgentByIdQuery : IRequest<object?>
{
    public Guid Id { get; set; }
}
