using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface ICreateShiftFromOrderService
{
    Task<Shift> CreateFromSealedOrder(Shift sealedOrder);
}
