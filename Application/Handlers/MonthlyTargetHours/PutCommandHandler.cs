// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a company-wide monthly target hours row. Refuses to move a row onto a month
/// that another active row already covers. After the commit it raises a
/// MonthlyTargetHoursChangedEvent for the old and the new month, so persisted works and breaks of
/// both affected months are recalculated.
/// </summary>
/// <param name="request">Contains the monthly target hours resource with year, month and hours</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Events;

namespace Klacks.Api.Application.Handlers.MonthlyTargetHours;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<MonthlyTargetHoursResource>, MonthlyTargetHoursResource?>
{
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PutCommandHandler(
        IMonthlyTargetHoursRepository monthlyTargetHoursRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<MonthlyTargetHoursResource?> Handle(PutCommand<MonthlyTargetHoursResource> request, CancellationToken cancellationToken)
    {
        var monthlyTargetHours = _scheduleMapper.ToMonthlyTargetHoursEntity(request.Resource);
        MonthlyTargetHoursValidator.Validate(monthlyTargetHours);

        (int Year, int Month)? previousMonth = null;

        var result = await ExecuteAsync(async () =>
        {
            var duplicate = await _monthlyTargetHoursRepository.GetByYearMonth(
                monthlyTargetHours.Year, monthlyTargetHours.Month);

            if (duplicate != null && duplicate.Id != monthlyTargetHours.Id)
            {
                throw new InvalidRequestException(
                    $"Monthly target hours for {monthlyTargetHours.Year}-{monthlyTargetHours.Month:00} already exist.");
            }

            var existing = await _monthlyTargetHoursRepository.Get(monthlyTargetHours.Id);
            if (existing != null)
            {
                previousMonth = (existing.Year, existing.Month);
            }

            var updated = await _monthlyTargetHoursRepository.Put(monthlyTargetHours);
            if (updated == null)
            {
                return null;
            }

            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToMonthlyTargetHoursResource(updated);
        },
        "updating monthly target hours",
        new { request.Resource?.Id, request.Resource?.Year, request.Resource?.Month });

        if (result != null)
        {
            var affectedMonths = previousMonth.HasValue
                ? new[] { previousMonth.Value, (result.Year, result.Month) }
                : new[] { (result.Year, result.Month) };
            await MonthlyTargetHoursChangeDispatcher.DispatchAsync(_eventDispatcher, _logger, affectedMonths);
        }

        return result;
    }
}
