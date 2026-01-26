using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteBreakCommand, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteCommandHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BreakResource?> Handle(DeleteBreakCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var breakEntry = await _breakRepository.Get(request.Id);
            if (breakEntry == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found.");
            }

            var (deletedBreak, periodHours) = await _breakRepository.DeleteWithPeriodHours(request.Id, request.PeriodStart, request.PeriodEnd);
            await _unitOfWork.CompleteAsync();

            var breakResource = _scheduleMapper.ToBreakResource(breakEntry);
            breakResource.PeriodHours = periodHours;

            return breakResource;
        },
        "deleting break",
        new { BreakId = request.Id });
    }
}
