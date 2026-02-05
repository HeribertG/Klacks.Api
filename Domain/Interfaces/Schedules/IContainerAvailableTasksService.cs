using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerAvailableTasksService
{
    Task<List<Shift>> GetAvailableTasksAsync(
        Guid containerId,
        int weekday,
        TimeOnly fromTime,
        TimeOnly untilTime,
        string? searchString = null,
        Guid? excludeContainerId = null,
        bool? isHoliday = null,
        bool? isWeekdayAndHoliday = null,
        CancellationToken cancellationToken = default);
}
