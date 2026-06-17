// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Human accept of a skill-relationship insight: a strong positive signal that boosts the edge
/// confidence (and its support), then recomputes its status. Returns null if the edge is unknown.
/// </summary>
/// <param name="repository">Persistence of the skill-relationship edges.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Services.Assistant.SkillGraph;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class AcceptSkillRelationCommandHandler : IRequestHandler<AcceptSkillRelationCommand, SkillRelationDto?>
{
    private readonly ISkillRelationRepository _repository;

    public AcceptSkillRelationCommandHandler(ISkillRelationRepository repository)
    {
        _repository = repository;
    }

    public async Task<SkillRelationDto?> Handle(AcceptSkillRelationCommand request, CancellationToken cancellationToken)
    {
        var edge = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (edge == null)
        {
            return null;
        }

        SkillRelationScoring.Adjust(edge, SkillGraphConstants.UserAcceptConfidenceBoost);
        edge.SupportCount += 1;
        edge.LastReinforcedAt = DateTime.UtcNow;
        edge.Source = SkillRelationSource.Learned;
        await _repository.UpdateAsync(edge, cancellationToken);

        return SkillRelationDto.From(edge);
    }
}
