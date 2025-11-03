using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Queries.ContainerTemplates;

public record GetAvailableTasksQuery(
    Guid ContainerId,
    int Weekday,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    string? SearchString = null,
    Guid? ExcludeContainerId = null,
    bool? IsHoliday = null,
    bool? IsWeekdayOrHoliday = null
) : IRequest<List<ShiftResource>>;
