using MediatR;

namespace Klacks_api.Commands.Settings.States;

public record PostCommand(Models.Settings.State model) : IRequest<Models.Settings.State>;
