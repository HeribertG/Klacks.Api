using MediatR;

namespace Klacks.Api.Application.Commands.Groups;

public record DeleteGroupNodeCommand(Guid Id) : IRequest<bool>;
