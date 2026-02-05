using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.Shifts;

public record GetShiftSchedulePartialQuery(ShiftSchedulePartialFilter Filter) : IRequest<ShiftScheduleResponse>;
