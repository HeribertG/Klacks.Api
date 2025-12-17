using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ShiftChanges;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ShiftChangeResource>, ShiftChangeResource?>
{
    private readonly IShiftChangeRepository _shiftChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IShiftChangeRepository shiftChangeRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _shiftChangeRepository = shiftChangeRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftChangeResource?> Handle(PostCommand<ShiftChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new ShiftChange");

        var shiftChange = _scheduleMapper.ToShiftChangeEntity(request.Resource);
        await _shiftChangeRepository.Add(shiftChange);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("ShiftChange created successfully with ID: {Id}", shiftChange.Id);
        return _scheduleMapper.ToShiftChangeResource(shiftChange);
    }
}
