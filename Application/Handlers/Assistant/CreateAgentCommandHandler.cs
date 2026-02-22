using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class CreateAgentCommandHandler : IRequestHandler<CreateAgentCommand, object>
{
    private readonly IAgentRepository _agentRepository;

    public CreateAgentCommandHandler(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository;
    }

    public async Task<object> Handle(CreateAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = new Agent
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            IsActive = true,
            IsDefault = false
        };

        await _agentRepository.AddAsync(agent, cancellationToken);
        return new { agent.Id, agent.Name };
    }
}
