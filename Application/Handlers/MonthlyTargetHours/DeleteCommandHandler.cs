// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting a company-wide monthly target hours row. Deleting a row simply removes the
/// override for that month, so no contract reference has to be checked.
/// </summary>
/// <param name="request">Contains the id of the monthly target hours row to delete</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.MonthlyTargetHours;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<MonthlyTargetHoursResource>, MonthlyTargetHoursResource?>
{
    private readonly IMonthlyTargetHoursRepository _monthlyTargetHoursRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IMonthlyTargetHoursRepository monthlyTargetHoursRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _monthlyTargetHoursRepository = monthlyTargetHoursRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<MonthlyTargetHoursResource?> Handle(DeleteCommand<MonthlyTargetHoursResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _monthlyTargetHoursRepository.Get(request.Id);
            if (existing == null)
            {
                _logger.LogWarning("MonthlyTargetHours not found: {MonthlyTargetHoursId}", request.Id);
                return null;
            }

            var resource = _scheduleMapper.ToMonthlyTargetHoursResource(existing);

            await _monthlyTargetHoursRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return resource;
        },
        "deleting monthly target hours",
        new { request.Id });
    }
}
