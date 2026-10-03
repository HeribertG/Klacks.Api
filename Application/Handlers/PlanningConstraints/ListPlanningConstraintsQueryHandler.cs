// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists planning constraints, optionally filtered by approval status (Proposed = the UI pending list).
/// </summary>
/// <param name="request">Optional status filter</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.PlanningConstraints;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

public class ListPlanningConstraintsQueryHandler : IRequestHandler<ListPlanningConstraintsQuery, IReadOnlyList<PlanningConstraintResource>>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly PlanningConstraintMapper _mapper;

    public ListPlanningConstraintsQueryHandler(IPlanningConstraintRepository repository, PlanningConstraintMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PlanningConstraintResource>> Handle(ListPlanningConstraintsQuery request, CancellationToken cancellationToken)
    {
        if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
        {
            throw new InvalidRequestException("Status must be a defined approval status.");
        }

        var constraints = await _repository.ListAsync(request.Status, cancellationToken);
        return _mapper.ToResources(constraints);
    }
}
