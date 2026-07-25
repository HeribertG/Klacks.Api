// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving a single individual period aggregate by id, including its period rows.
/// </summary>
/// <param name="request">Contains the id of the individual period to retrieve</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.IndividualPeriods;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<IndividualPeriodResource>, IndividualPeriodResource>
{
    private readonly IIndividualPeriodRepository _individualPeriodRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(IIndividualPeriodRepository individualPeriodRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _individualPeriodRepository = individualPeriodRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IndividualPeriodResource> Handle(GetQuery<IndividualPeriodResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var individualPeriod = await _individualPeriodRepository.Get(request.Id);

            if (individualPeriod == null)
            {
                throw new KeyNotFoundException($"IndividualPeriod with ID {request.Id} not found");
            }

            return _scheduleMapper.ToIndividualPeriodResource(individualPeriod);
        }, nameof(Handle), new { request.Id });
    }
}
