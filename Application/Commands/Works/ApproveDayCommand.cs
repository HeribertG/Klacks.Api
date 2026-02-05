using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record ApproveDayCommand(DateOnly Date, Guid GroupId) : IRequest<int>;
