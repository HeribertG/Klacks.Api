// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the skill-relationship edges Klacksy actually learned, ordered by confidence, for the insight view.
/// Only learned edges are shown: substrate-prior seeds stay hidden until real usage or a human accept graduates
/// them to learned (see SkillRelationLearner.Reinforce and AcceptSkillRelationCommandHandler). Retired edges are
/// dropped. This keeps the view a list of genuine, curatable insights rather than a dump of every seeded pair.
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
        return relation.Status != SkillRelationStatus.Retired
            && relation.Source == SkillRelationSource.Learned;
    }
}
