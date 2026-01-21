using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PeriodHoursBackgroundService _periodHoursBackgroundService;

    public PutCommandHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        PeriodHoursBackgroundService periodHoursBackgroundService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursBackgroundService = periodHoursBackgroundService;
    }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = _scheduleMapper.ToBreakEntity(request.Resource);
            var updated = await _breakRepository.Put(entity);

            if (updated == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found");
            }

            await _unitOfWork.CompleteAsync();

            _periodHoursBackgroundService.QueueRecalculation(
                updated.ClientId,
                DateOnly.FromDateTime(updated.CurrentDate));

            return _scheduleMapper.ToBreakResource(updated);
        }, "UpdateBreak", new { request.Resource.Id });
    }
}
