// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for loading all container shift overrides in a date range (for shift-section icons).
/// </summary>
/// <param name="containerId">The container shift</param>
/// <param name="fromDate">Start of the date range</param>
/// <param name="toDate">End of the date range</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerShiftOverrides;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerShiftOverrides;

public class GetContainerShiftOverridesForRangeQueryHandler : IRequestHandler<GetContainerShiftOverridesForRangeQuery, List<ContainerShiftOverrideResource>>
{
    private readonly IContainerShiftOverrideRepository _repository;
    private readonly ScheduleMapper _mapper;

    public GetContainerShiftOverridesForRangeQueryHandler(
        IContainerShiftOverrideRepository repository,
        ScheduleMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<ContainerShiftOverrideResource>> Handle(GetContainerShiftOverridesForRangeQuery request, CancellationToken cancellationToken)
    {
        var entities = await _repository.GetByContainerAndDateRange(request.ContainerId, request.FromDate, request.ToDate, cancellationToken);
        return entities.Select(e => _mapper.ToContainerShiftOverrideResource(e)).ToList();
    }
}
