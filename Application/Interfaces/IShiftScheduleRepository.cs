using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftScheduleRepository
{
    Task<List<ShiftDayAssignment>> GetShiftScheduleAsync(
        ShiftScheduleFilter filter,
        CancellationToken cancellationToken);
}
