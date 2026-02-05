using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PeriodClosures;

public record ReopenPeriodCommand(Guid Id) : IRequest<bool>;
