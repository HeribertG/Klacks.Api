using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostCommandHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BreakResource?> Handle(PostCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        var entity = _scheduleMapper.ToBreakEntity(request.Resource);

        DateOnly periodStart;
        DateOnly periodEnd;

        if (request.Resource.PeriodStart.HasValue && request.Resource.PeriodEnd.HasValue)
        {
            periodStart = request.Resource.PeriodStart.Value;
            periodEnd = request.Resource.PeriodEnd.Value;
        }
        else
        {
            (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(entity.CurrentDate));
        }

        var (createdBreak, periodHours) = await _breakRepository.AddWithPeriodHours(entity, periodStart, periodEnd);
        await _unitOfWork.CompleteAsync();

        var breakResource = _scheduleMapper.ToBreakResource(createdBreak);
        breakResource.PeriodHours = periodHours;

        return breakResource;
    }
}
