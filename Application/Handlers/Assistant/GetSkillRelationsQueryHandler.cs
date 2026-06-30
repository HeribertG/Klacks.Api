// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns only Candidate skill-relationship edges Klacksy learned, ordered by confidence, for the insight view.
/// Only learned, undecided (Candidate) edges are shown — Active (accepted) and Retired (dismissed) edges are
/// excluded so the list shows only insights that still need a human decision.
/// </summary>
/// <param name="repository">Source of the skill-relationship edges.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetSkillRelationsQueryHandler : IRequestHandler<GetSkillRelationsQuery, List<SkillRelationDto>>
{
    private readonly ISkillRelationRepository _repository;

    public GetSkillRelationsQueryHandler(ISkillRelationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SkillRelationDto>> Handle(GetSkillRelationsQuery request, CancellationToken cancellationToken)
    {
        var relations = await _repository.GetAllAsync(cancellationToken);
        return relations
            .Where(IsLearnedInsight)
            .OrderByDescending(r => r.Confidence)
            .Select(SkillRelationDto.From)
            .ToList();
    }

    private static bool IsLearnedInsight(SkillRelation relation)
    {
        return relation.Status == SkillRelationStatus.Candidate
            && relation.Source == SkillRelationSource.Learned;
    }
}
