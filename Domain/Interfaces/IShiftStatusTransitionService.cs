using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftStatusTransitionService
{
    Task<Shift> HandleReadyToCutTransition(Shift shift);
}
