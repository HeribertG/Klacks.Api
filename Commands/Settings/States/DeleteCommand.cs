using MediatR;

namespace Klacks.Api.Commands.Settings.States;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.State>;
