// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing all company-wide monthly target hours rows, ordered by year and month.
/// </summary>
/// <param name="request">Empty list query, no filter parameters</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.MonthlyTargetHours;

public class ListQueryHandler : IRequestHandler<ListQuery<MonthlyTargetHoursResource>, IEnumerable<MonthlyTargetHoursResource>>
{
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(IMonthlyTargetHoursRepository monthlyTargetHoursRepository, ScheduleMapper scheduleMapper)
    {
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<MonthlyTargetHoursResource>> Handle(ListQuery<MonthlyTargetHoursResource> request, CancellationToken cancellationToken)
    {
        var monthlyTargetHours = await _monthlyTargetHoursRepository.List();
        return monthlyTargetHours.Select(_scheduleMapper.ToMonthlyTargetHoursResource);
    }
}
