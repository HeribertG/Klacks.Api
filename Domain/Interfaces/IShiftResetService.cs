using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftResetService
{
    Task<Shift> CreateNewOriginalShiftFromSealedOrderAsync(Shift sealedOrder, DateOnly newStartDate);
    void CloseExistingSplitShifts(List<Shift> shifts, DateOnly closeDate);
}
