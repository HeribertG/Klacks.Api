// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers, for a set of entity ids the client currently has on screen, which of them carry an Executed
/// condition-ledger row - the service grid's "Klacksy handled this one" marker. Two rules decide what
/// comes back. First, visibility: the scope is resolved for the requesting user through the same
/// IAgentConditionScopeResolver every other ledger read uses, and a caller who is not a planner at all
/// gets an empty list rather than a Forbidden, matching how the scoped ledger reads hide rows instead of
/// confirming they exist. Second, multiplicity: one entity can carry several Executed rows (a re-detected
/// condition opens a fresh row beside the executed one, and the fingerprint carries a business date), so
/// the newest handling per entity wins - the repository already orders newest first, and this keeps the
/// first row it sees per id, which is what a single per-cell marker needs.
/// </summary>
/// <param name="scopeResolver">Resolves whether the requesting user is a planner and which group roots they see.</param>
/// <param name="conditionRepository">Scoped read of the Executed ledger rows for the requested entity ids.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetConditionAttributionsQueryHandler
    : IRequestHandler<GetConditionAttributionsQuery, IReadOnlyList<ConditionAttributionDto>>
{
    private static readonly IReadOnlyList<ConditionAttributionDto> NoAttributions = [];

    private readonly IAgentConditionScopeResolver _scopeResolver;
    private readonly IAgentConditionRepository _conditionRepository;

    public GetConditionAttributionsQueryHandler(
        IAgentConditionScopeResolver scopeResolver,
        IAgentConditionRepository conditionRepository)
    {
        _scopeResolver = scopeResolver;
        _conditionRepository = conditionRepository;
    }

    public async Task<IReadOnlyList<ConditionAttributionDto>> Handle(
        GetConditionAttributionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.EntityIds.Count == 0)
        {
            return NoAttributions;
        }

        var scope = await _scopeResolver.ResolveAsync(request.UserId, cancellationToken);
        if (!scope.IsPlanner)
        {
            return NoAttributions;
        }

        var rows = await _conditionRepository.GetExecutedForEntitiesAsync(
            request.EntityIds, scope.IsUnrestricted, scope.VisibleRootIds, cancellationToken);

        return NewestPerEntity(rows);
    }

    private static IReadOnlyList<ConditionAttributionDto> NewestPerEntity(IReadOnlyList<AgentCondition> rows)
    {
        var attributions = new List<ConditionAttributionDto>(rows.Count);
        var alreadyAttributed = new HashSet<Guid>();

        foreach (var row in rows)
        {
            if (row.EntityId is not Guid entityId || !alreadyAttributed.Add(entityId))
            {
                continue;
            }

            attributions.Add(new ConditionAttributionDto
            {
                EntityId = entityId,
                HandledAtUtc = row.HandledAtUtc,
                TriggerKind = row.TriggerKind
            });
        }

        return attributions;
    }
}
