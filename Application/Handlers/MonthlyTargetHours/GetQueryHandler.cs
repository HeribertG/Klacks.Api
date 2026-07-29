// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving a single company-wide monthly target hours row by id.
/// </summary>
/// <param name="request">Contains the id of the monthly target hours row to retrieve</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.MonthlyTargetHours;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<MonthlyTargetHoursResource>, MonthlyTargetHoursResource>
{
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IMonthlyTargetHoursRepository monthlyTargetHoursRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<MonthlyTargetHoursResource> Handle(GetQuery<MonthlyTargetHoursResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var monthlyTargetHours = await _monthlyTargetHoursRepository.Get(request.Id);

            if (monthlyTargetHours == null)
            {
                throw new KeyNotFoundException($"MonthlyTargetHours with ID {request.Id} not found");
            }

            return _scheduleMapper.ToMonthlyTargetHoursResource(monthlyTargetHours);
        }, nameof(Handle), new { request.Id });
    }
}
