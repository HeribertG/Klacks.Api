using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class CreateShiftFromOrderService : ICreateShiftFromOrderService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateShiftFromOrderService> _logger;
    private readonly IShiftValidator _shiftValidator;

    public CreateShiftFromOrderService(
        IShiftRepository shiftRepository,
        IMapper mapper,
        ILogger<CreateShiftFromOrderService> logger,
        IShiftValidator shiftValidator)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
        _logger = logger;
        _shiftValidator = shiftValidator;
    }

    public async Task<Shift> CreateFromSealedOrder(Shift sealedOrder)
    {
        if (sealedOrder.Status != ShiftStatus.SealedOrder)
        {
            return sealedOrder;
        }

        var sealedOrderId = sealedOrder.Id;

        _logger.LogInformation(
            "Creating OriginalShift from SealedOrder: SealedOrderId={SealedOrderId}",
            sealedOrderId);

        await _shiftValidator.EnsureNoOriginalShiftCopyExists(sealedOrderId, _shiftRepository);

        var originalShift = _mapper.Map<Shift>(sealedOrder);

        originalShift.Status = ShiftStatus.OriginalShift;
        originalShift.OriginalId = sealedOrderId;

        await _shiftRepository.Add(originalShift);

        _logger.LogInformation(
            "Created OriginalShift: NewShiftId={NewShiftId}, OriginalId={OriginalId}, Status={Status}",
            originalShift.Id, originalShift.OriginalId, originalShift.Status);

        return originalShift;
    }
}
