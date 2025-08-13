using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftApplicationService
{
    Task<ShiftResource?> GetShiftByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TruncatedShiftResource> GetTruncatedShiftsAsync(ShiftFilter filter, CancellationToken cancellationToken = default);
    Task<List<ShiftResource>> GetShiftCutsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShiftResource> CreateShiftAsync(ShiftResource shiftResource, CancellationToken cancellationToken = default);
    Task<ShiftResource> UpdateShiftAsync(ShiftResource shiftResource, CancellationToken cancellationToken = default);
    Task<List<ShiftResource>> CreateShiftCutsAsync(List<ShiftResource> shiftResources, CancellationToken cancellationToken = default);
    Task<List<ShiftResource>> UpdateShiftCutsAsync(List<ShiftResource> shiftResources, CancellationToken cancellationToken = default);
}