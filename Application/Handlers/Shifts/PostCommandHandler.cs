using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftStatusTransitionService _statusTransitionService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IShiftRepository shiftRepository,
        IShiftStatusTransitionService statusTransitionService,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _statusTransitionService = statusTransitionService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftResource?> Handle(PostCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new shift: Name={Name}, MacroId={MacroId}, ClientId={ClientId}, GroupCount={GroupCount}",
            request.Resource.Name, request.Resource.MacroId, request.Resource.ClientId, request.Resource.Groups?.Count ?? 0);

        var shift = _mapper.Map<Shift>(request.Resource);

        if (shift.Status == ShiftStatus.SealedOrder)
        {
            _logger.LogInformation("Shift has SealedOrder status, will create OriginalShift copy after save: Name={Name}", shift.Name);
        }

        await _shiftRepository.Add(shift);
        await _unitOfWork.CompleteAsync();

        Shift resultShift = shift;

        if (shift.Status == ShiftStatus.SealedOrder)
        {
            resultShift = await _statusTransitionService.HandleReadyToCutTransition(shift);
            await _unitOfWork.CompleteAsync();
        }

        _logger.LogInformation("Shift created successfully: Id={ShiftId}, Name={Name}, Status={Status}",
            resultShift.Id, resultShift.Name, resultShift.Status);

        return _mapper.Map<ShiftResource>(resultShift);
    }
}
