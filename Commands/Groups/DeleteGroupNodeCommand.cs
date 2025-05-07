using MediatR;

namespace Klacks.Api.Commands.Groups;

public record DeleteGroupNodeCommand(Guid Id) : IRequest<bool>;
