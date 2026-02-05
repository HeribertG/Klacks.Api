using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.PeriodHours;

public record GetPeriodHoursQuery(PeriodHoursRequest Request) : IRequest<Dictionary<Guid, PeriodHoursResource>>;
