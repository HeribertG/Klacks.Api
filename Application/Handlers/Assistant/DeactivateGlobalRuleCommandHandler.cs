// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DeactivateGlobalRuleCommandHandler : IRequestHandler<DeactivateGlobalRuleCommand, Unit>
{
    private readonly IGlobalAgentRuleRepository _ruleRepository;

    public DeactivateGlobalRuleCommandHandler(IGlobalAgentRuleRepository ruleRepository)
    {
        _ruleRepository = ruleRepository;
    }

    public async Task<Unit> Handle(DeactivateGlobalRuleCommand request, CancellationToken cancellationToken)
    {
        await _ruleRepository.DeactivateRuleAsync(request.Name, cancellationToken);
        return Unit.Value;
    }
}
