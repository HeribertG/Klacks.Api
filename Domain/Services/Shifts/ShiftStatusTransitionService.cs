using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftStatusTransitionService : IShiftStatusTransitionService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ShiftStatusTransitionService> _logger;
    private readonly IShiftValidator _shiftValidator;

    public ShiftStatusTransitionService(
        IShiftRepository shiftRepository,
        IMapper mapper,
        ILogger<ShiftStatusTransitionService> logger,
        IShiftValidator shiftValidator)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
        _logger = logger;
        _shiftValidator = shiftValidator;
    }

    public async Task<Shift> HandleReadyToCutTransition(Shift originalShift)
    {
        if (originalShift.Status != ShiftStatus.SealedOrder)
        {
            return originalShift;
        }

        var originalShiftId = originalShift.Id;

        _logger.LogInformation(
            "Processing SealedOrder transition: Creating new OriginalShift based on OriginalShiftId={OriginalShiftId}",
            originalShiftId);

        await _shiftValidator.EnsureNoOriginalShiftCopyExists(originalShiftId, _shiftRepository);

        var cutOriginalShift = _mapper.Map<Shift>(originalShift);

        cutOriginalShift.Status = ShiftStatus.OriginalShift;
        cutOriginalShift.OriginalId = originalShiftId;

        await _shiftRepository.Add(cutOriginalShift);

        _logger.LogInformation(
            "Created new OriginalShift: NewShiftId={NewShiftId}, OriginalId={OriginalId}, Status={Status}",
            cutOriginalShift.Id, cutOriginalShift.OriginalId, cutOriginalShift.Status);

        return cutOriginalShift;
    }
}
