using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkChangeResultService
{
    Task<WorkChangeClientResult> GetClientResultAsync(
        Guid clientId,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly threeDayStart,
        DateOnly threeDayEnd,
        CancellationToken cancellationToken);
}
