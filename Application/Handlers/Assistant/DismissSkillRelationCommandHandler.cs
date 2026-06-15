// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Human dismiss of a skill-relationship insight: a strong negative signal that drops the edge
/// confidence (and counts a contradiction), then recomputes its status — this is how the learning
/// loop is taught to stop suggesting a relationship. Returns null if the edge is unknown.
/// </summary>
/// <param name="repository">Persistence of the skill-relationship edges.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Services.Assistant.SkillGraph;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DismissSkillRelationCommandHandler : IRequestHandler<DismissSkillRelationCommand, SkillRelationDto?>
{
    private readonly ISkillRelationRepository _repository;

    public DismissSkillRelationCommandHandler(ISkillRelationRepository repository)
    {
        _repository = repository;
    }

    public async Task<SkillRelationDto?> Handle(DismissSkillRelationCommand request, CancellationToken cancellationToken)
    {
        var edge = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (edge == null)
        {
            return null;
        }

        SkillRelationScoring.Adjust(edge, -SkillGraphConstants.UserDismissConfidencePenalty);
        edge.ContradictionCount += 1;
        await _repository.UpdateAsync(edge, cancellationToken);

        return SkillRelationDto.From(edge);
    }
}
