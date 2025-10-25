using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftResetService : IShiftResetService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ShiftResetService> _logger;

    public ShiftResetService(
        IShiftRepository shiftRepository,
        IMapper mapper,
        ILogger<ShiftResetService> logger)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Shift> CreateNewOriginalShiftFromSealedOrderAsync(Shift sealedOrder, DateOnly newStartDate)
    {
        _logger.LogInformation("Creating new OriginalShift from SealedOrder: {SealedOrderId}, NewStartDate: {NewStartDate}",
            sealedOrder.Id, newStartDate);

        var newOriginalShift = _mapper.Map<Shift>(sealedOrder);
        newOriginalShift.OriginalId = sealedOrder.Id;
        newOriginalShift.Status = ShiftStatus.OriginalShift;
        newOriginalShift.FromDate = newStartDate;
        newOriginalShift.UntilDate = sealedOrder.UntilDate;
        newOriginalShift.ParentId = null;
        newOriginalShift.RootId = null;
        newOriginalShift.Lft = null;
        newOriginalShift.Rgt = null;

        _logger.LogInformation("New OriginalShift created: ID={Id}, OriginalId={OriginalId}, FromDate={FromDate}, GroupItems count: {Count}",
            newOriginalShift.Id, newOriginalShift.OriginalId, newOriginalShift.FromDate, newOriginalShift.GroupItems.Count);

        await _shiftRepository.Add(newOriginalShift);

        return newOriginalShift;
    }

    public void CloseExistingSplitShifts(List<Shift> shifts, DateOnly closeDate)
    {
        _logger.LogInformation("Closing existing SplitShifts with UntilDate: {CloseDate}", closeDate);

        var splitShifts = shifts.Where(s => s.Status == ShiftStatus.SplitShift).ToList();

        foreach (var shift in splitShifts)
        {
            shift.UntilDate = closeDate;
            _logger.LogInformation("Updated SplitShift {ShiftId} UntilDate to {UntilDate}",
                shift.Id, closeDate);
        }

        _logger.LogInformation("Closed {Count} SplitShifts", splitShifts.Count);
    }
}
