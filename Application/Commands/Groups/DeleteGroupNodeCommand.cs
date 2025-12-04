using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

public record DeleteGroupNodeCommand(Guid Id) : IRequest<bool>;
