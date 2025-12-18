using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.WorkSchedule;

public record GetWorkScheduleQuery(WorkScheduleFilter Filter) : IRequest<WorkScheduleResponse>;
