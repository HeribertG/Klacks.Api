using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.Shifts;

public record GetShiftScheduleQuery(ShiftScheduleFilter Filter) : IRequest<List<ShiftScheduleResource>>;
