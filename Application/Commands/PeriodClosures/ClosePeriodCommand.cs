using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.PeriodClosures;

public record ClosePeriodCommand(DateOnly StartDate, DateOnly EndDate) : IRequest<PeriodClosureResource>;
