// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one planning constraint; null when it does not exist or is deleted.
/// </summary>
/// <param name="request">Constraint id</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.PlanningConstraints;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

public class GetPlanningConstraintQueryHandler : IRequestHandler<GetPlanningConstraintQuery, PlanningConstraintResource?>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly PlanningConstraintMapper _mapper;

    public GetPlanningConstraintQueryHandler(IPlanningConstraintRepository repository, PlanningConstraintMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PlanningConstraintResource?> Handle(GetPlanningConstraintQuery request, CancellationToken cancellationToken)
    {
        var constraint = await _repository.GetAsync(request.Id, cancellationToken);
        return constraint is null ? null : _mapper.ToResource(constraint);
    }
}
