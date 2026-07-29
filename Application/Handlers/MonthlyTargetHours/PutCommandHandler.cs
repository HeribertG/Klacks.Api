// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a company-wide monthly target hours row. Refuses to move a row onto a month
/// that another active row already covers.
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

namespace Klacks.Api.Application.Handlers.MonthlyTargetHours;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<MonthlyTargetHoursResource>, MonthlyTargetHoursResource?>
{
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IMonthlyTargetHoursRepository monthlyTargetHoursRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<MonthlyTargetHoursResource?> Handle(PutCommand<MonthlyTargetHoursResource> request, CancellationToken cancellationToken)
    {
        var monthlyTargetHours = _scheduleMapper.ToMonthlyTargetHoursEntity(request.Resource);
        MonthlyTargetHoursValidator.Validate(monthlyTargetHours);

        return await ExecuteAsync(async () =>
        {
            var duplicate = await _monthlyTargetHoursRepository.GetByYearMonth(
                monthlyTargetHours.Year, monthlyTargetHours.Month);

            if (duplicate != null && duplicate.Id != monthlyTargetHours.Id)
            {
                throw new InvalidRequestException(
                    $"Monthly target hours for {monthlyTargetHours.Year}-{monthlyTargetHours.Month:00} already exist.");
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
    }
}
