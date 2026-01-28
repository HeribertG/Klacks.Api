using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.ScheduleEntries;

public record GetScheduleEntriesQuery(WorkScheduleFilter Filter) : IRequest<WorkScheduleResponse>;
