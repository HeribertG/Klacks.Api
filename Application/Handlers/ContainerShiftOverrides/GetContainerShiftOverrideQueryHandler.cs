// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for loading a single container shift override by container ID and date. A missing or hidden container is
/// answered with 404 (KeyNotFoundException); a visible container without an override for the date returns null.
/// </summary>
/// <param name="containerId">The container shift to look up</param>
/// <param name="date">The specific date</param>
/// <param name="containerVisibilityGuard">Refuses missing and hidden containers alike</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerShiftOverrides;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class GetContainerShiftOverrideQueryHandler : IRequestHandler<GetContainerShiftOverrideQuery, ContainerShiftOverrideResource?>
{
    private readonly IContainerShiftOverrideRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IContainerVisibilityGuard _containerVisibilityGuard;

    public GetContainerShiftOverrideQueryHandler(
        IContainerShiftOverrideRepository repository,
        ScheduleMapper mapper,
        IContainerVisibilityGuard containerVisibilityGuard)
    {
        _repository = repository;
        _mapper = mapper;
        _containerVisibilityGuard = containerVisibilityGuard;
    }

    public async Task<ContainerShiftOverrideResource?> Handle(GetContainerShiftOverrideQuery request, CancellationToken cancellationToken)
    {
        await _containerVisibilityGuard.EnsureContainerVisibleAsync(request.ContainerId, cancellationToken);

        var entity = await _repository.GetByContainerAndDateWithItems(request.ContainerId, request.Date, cancellationToken);
        if (entity is null) return null;

        var hasWork = await _repository.HasWorkForOverride(request.ContainerId, request.Date, cancellationToken);
        return _mapper.ToContainerShiftOverrideResource(entity, hasWork);
    }
}
