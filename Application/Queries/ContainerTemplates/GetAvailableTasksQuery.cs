using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ContainerTemplates;

public record GetAvailableTasksQuery(
    Guid ContainerId,
    int Weekday,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    string? SearchString = null,
    Guid? ExcludeContainerId = null,
    bool? IsHoliday = null,
    bool? IsWeekdayAndHoliday = null
) : IRequest<List<ShiftResource>>;
