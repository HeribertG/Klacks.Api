// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpsertGlobalRuleCommandHandler : IRequestHandler<UpsertGlobalRuleCommand, object>
{
    private readonly IGlobalAgentRuleRepository _ruleRepository;

    public UpsertGlobalRuleCommandHandler(IGlobalAgentRuleRepository ruleRepository)
    {
        _ruleRepository = ruleRepository;
    }

    public async Task<object> Handle(UpsertGlobalRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.UpsertRuleAsync(
            request.Name, request.Content,
            request.SortOrder ?? 0,
            source: null, changedBy: request.UserId, cancellationToken: cancellationToken);

        return new { rule.Name, rule.Version };
    }
}
