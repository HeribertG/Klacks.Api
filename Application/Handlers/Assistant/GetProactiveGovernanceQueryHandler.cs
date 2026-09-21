// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the effective governance of every governed trigger kind plus the global kill-switch state.
/// Kinds that were never configured are reported with their fail-safe defaults rather than omitted, so
/// the settings card shows a complete table and no kind silently lacks a rule.
/// </summary>
/// <param name="resolver">Folds stored rules, defaults and the kill switch into one decision per kind.</param>
/// <param name="remediationRegistry">Says per kind whether a scenario could be prepared at all.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetProactiveGovernanceQueryHandler : IRequestHandler<GetProactiveGovernanceQuery, ProactiveGovernanceDto>
{
    private readonly IProactiveGovernanceResolver _resolver;
    private readonly IConditionRemediationRegistry _remediationRegistry;

    public GetProactiveGovernanceQueryHandler(
        IProactiveGovernanceResolver resolver, IConditionRemediationRegistry remediationRegistry)
    {
        _resolver = resolver;
        _remediationRegistry = remediationRegistry;
    }

    public async Task<ProactiveGovernanceDto> Handle(
        GetProactiveGovernanceQuery request, CancellationToken cancellationToken)
    {
        var killSwitchActive = await _resolver.IsKillSwitchActiveAsync(cancellationToken);
        var globalAutonomyLevel = await _resolver.GetGlobalAutonomyLevelAsync(cancellationToken);
        var decisions = await _resolver.ResolveAllAsync(cancellationToken);
        return ProactiveGovernanceDtoMapper.ToDto(
            killSwitchActive, globalAutonomyLevel, decisions, _remediationRegistry);
    }
}
