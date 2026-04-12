// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for loading a single container shift override by container ID and date.
/// </summary>
/// <param name="containerId">The container shift to look up</param>
/// <param name="date">The specific date</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerShiftOverrides;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class GetContainerShiftOverrideQueryHandler : IRequestHandler<GetContainerShiftOverrideQuery, ContainerShiftOverrideResource?>
{
    private readonly IContainerShiftOverrideRepository _repository;
    private readonly ScheduleMapper _mapper;

    public GetContainerShiftOverrideQueryHandler(
        IContainerShiftOverrideRepository repository,
        ScheduleMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ContainerShiftOverrideResource?> Handle(GetContainerShiftOverrideQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByContainerAndDateWithItems(request.ContainerId, request.Date, cancellationToken);
        if (entity is null) return null;

        var hasWork = await _repository.HasWorkForOverride(request.ContainerId, request.Date, cancellationToken);
        return _mapper.ToContainerShiftOverrideResource(entity, hasWork);
    }
}
