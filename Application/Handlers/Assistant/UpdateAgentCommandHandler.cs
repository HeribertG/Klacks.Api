// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpdateAgentCommandHandler : IRequestHandler<UpdateAgentCommand, object?>
{
    private readonly IAgentRepository _agentRepository;

    public UpdateAgentCommandHandler(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository;
    }

    public async Task<object?> Handle(UpdateAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (agent == null) return null;

        if (request.Name != null) agent.Name = request.Name;
        if (request.DisplayName != null) agent.DisplayName = request.DisplayName;
        if (request.Description != null) agent.Description = request.Description;
        if (request.IsActive.HasValue) agent.IsActive = request.IsActive.Value;

        await _agentRepository.UpdateAsync(agent, cancellationToken);
        return new { agent.Id, agent.Name, agent.DisplayName, agent.IsActive };
    }
}
