using MediatR;

namespace Klacks_api.Commands.Settings.States;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.State>;
