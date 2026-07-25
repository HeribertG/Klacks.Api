// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing all individual period aggregates, including their period rows.
/// </summary>
/// <param name="request">Empty list query, no filter parameters</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.IndividualPeriods;

public class ListQueryHandler : IRequestHandler<ListQuery<IndividualPeriodResource>, IEnumerable<IndividualPeriodResource>>
{
    private readonly IIndividualPeriodRepository _individualPeriodRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(IIndividualPeriodRepository individualPeriodRepository, ScheduleMapper scheduleMapper)
    {
        _individualPeriodRepository = individualPeriodRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<IndividualPeriodResource>> Handle(ListQuery<IndividualPeriodResource> request, CancellationToken cancellationToken)
    {
        var individualPeriods = await _individualPeriodRepository.List();
        return individualPeriods.Select(_scheduleMapper.ToIndividualPeriodResource);
    }
}
