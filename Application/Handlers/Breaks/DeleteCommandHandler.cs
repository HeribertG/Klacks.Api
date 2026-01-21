using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PeriodHoursBackgroundService _periodHoursBackgroundService;

    public DeleteCommandHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        PeriodHoursBackgroundService periodHoursBackgroundService,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursBackgroundService = periodHoursBackgroundService;
    }

    public async Task<BreakResource?> Handle(DeleteCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _breakRepository.Get(request.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found");
            }

            var clientId = existing.ClientId;
            var currentDate = existing.CurrentDate;

            var deleted = await _breakRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            _periodHoursBackgroundService.QueueRecalculation(
                clientId,
                DateOnly.FromDateTime(currentDate));

            return deleted != null ? _scheduleMapper.ToBreakResource(deleted) : null;
        }, "DeleteBreak", new { request.Id });
    }
}
