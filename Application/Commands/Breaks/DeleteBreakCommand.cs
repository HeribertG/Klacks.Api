using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Breaks;

public record DeleteBreakCommand(
    Guid Id,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<BreakResource?>;
