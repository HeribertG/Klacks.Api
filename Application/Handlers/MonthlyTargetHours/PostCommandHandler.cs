// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a company-wide monthly target hours row. Refuses a month that already has
/// an active row, since a month can only be overridden once. After the commit it raises a
/// MonthlyTargetHoursChangedEvent so persisted works and breaks of that month are recalculated.
/// </summary>
/// <param name="request">Contains the monthly target hours resource with year, month and hours</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.MonthlyTargetHours;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<MonthlyTargetHoursResource>, MonthlyTargetHoursResource?>
{
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PostCommandHandler(
        IMonthlyTargetHoursRepository monthlyTargetHoursRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<MonthlyTargetHoursResource?> Handle(PostCommand<MonthlyTargetHoursResource> request, CancellationToken cancellationToken)
    {
        var monthlyTargetHours = _scheduleMapper.ToMonthlyTargetHoursEntity(request.Resource);
        MonthlyTargetHoursValidator.Validate(monthlyTargetHours);

        var result = await ExecuteAsync(async () =>
        {
            var duplicate = await _monthlyTargetHoursRepository.GetByYearMonth(
                monthlyTargetHours.Year, monthlyTargetHours.Month);

            if (duplicate != null)
            {
                throw new InvalidRequestException(
                    $"Monthly target hours for {monthlyTargetHours.Year}-{monthlyTargetHours.Month:00} already exist.");
            }

            await _monthlyTargetHoursRepository.Add(monthlyTargetHours);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToMonthlyTargetHoursResource(monthlyTargetHours);
        },
        "creating monthly target hours",
        new { request.Resource?.Id, request.Resource?.Year, request.Resource?.Month });

        if (result != null)
        {
            await MonthlyTargetHoursChangeDispatcher.DispatchAsync(_eventDispatcher, _logger, (result.Year, result.Month));
        }

        return result;
    }
}
