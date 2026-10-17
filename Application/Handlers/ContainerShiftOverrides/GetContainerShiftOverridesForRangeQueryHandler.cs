// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for loading all container shift overrides in a date range (for shift-section icons). A missing or hidden
/// container is answered with 404 (KeyNotFoundException).
/// </summary>
/// <param name="containerId">The container shift</param>
/// <param name="fromDate">Start of the date range</param>
/// <param name="toDate">End of the date range</param>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers alike</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerShiftOverrides;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class GetContainerShiftOverridesForRangeQueryHandler : IRequestHandler<GetContainerShiftOverridesForRangeQuery, List<ContainerShiftOverrideResource>>
{
    private readonly IContainerShiftOverrideRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;

    public GetContainerShiftOverridesForRangeQueryHandler(
        IContainerShiftOverrideRepository repository,
        ScheduleMapper mapper,
        IContainerVisibilityGuard containerVisibilityGuard)
    {
        _repository = repository;
        _mapper = mapper;
        _containerVisibilityGuard = containerVisibilityGuard;
    }

    public async Task<List<ContainerShiftOverrideResource>> Handle(GetContainerShiftOverridesForRangeQuery request, CancellationToken cancellationToken)
    {
        await _containerVisibilityGuard.EnsureContainerVisibleAsync(request.ContainerId, cancellationToken);

        var entities = await _repository.GetByContainerAndDateRange(request.ContainerId, request.FromDate, request.ToDate, cancellationToken);
        var result = new List<ContainerShiftOverrideResource>(entities.Count);

        foreach (var entity in entities)
        {
            var hasWork = await _repository.HasWorkForOverride(entity.ContainerId, entity.Date, cancellationToken);
            result.Add(_mapper.ToContainerShiftOverrideResource(entity, hasWork));
        }

        return result;
    }
}
