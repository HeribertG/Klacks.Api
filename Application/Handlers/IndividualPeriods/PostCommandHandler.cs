// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating an individual period aggregate, including its period rows.
/// </summary>
/// <param name="request">Contains the individual period resource with name and period rows</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.IndividualPeriods;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<IndividualPeriodResource>, IndividualPeriodResource?>
{
    private readonly IIndividualPeriodRepository _individualPeriodRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IIndividualPeriodRepository individualPeriodRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _individualPeriodRepository = individualPeriodRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IndividualPeriodResource?> Handle(PostCommand<IndividualPeriodResource> request, CancellationToken cancellationToken)
    {
        var individualPeriod = _scheduleMapper.ToIndividualPeriodEntity(request.Resource);
        IndividualPeriodValidator.Validate(individualPeriod);

        return await ExecuteAsync(async () =>
        {
            await _individualPeriodRepository.Add(individualPeriod);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToIndividualPeriodResource(individualPeriod);
        },
        "creating individual period",
        new { request.Resource?.Id, request.Resource?.Name });
    }
}
