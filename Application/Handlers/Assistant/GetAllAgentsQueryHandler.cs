using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAllAgentsQueryHandler : IRequestHandler<GetAllAgentsQuery, object>
{
    private readonly IAgentRepository _agentRepository;

    public GetAllAgentsQueryHandler(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository;
    }

    public async Task<object> Handle(GetAllAgentsQuery request, CancellationToken cancellationToken)
    {
        var agents = await _agentRepository.GetAllAsync(cancellationToken);
        return agents.Select(a => new
        {
            a.Id, a.Name, a.DisplayName, a.Description,
            a.IsActive, a.IsDefault, a.CreateTime
        }).ToList();
    }
}
