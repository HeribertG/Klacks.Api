// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the non-retired skill-relationship edges ordered by confidence for the insight view.
/// </summary>
/// <param name="repository">Source of the skill-relationship edges.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
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
            .Where(r => r.Status != SkillRelationStatus.Retired)
            .OrderByDescending(r => r.Confidence)
            .Select(SkillRelationDto.From)
            .ToList();
    }
}
