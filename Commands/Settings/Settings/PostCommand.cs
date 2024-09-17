using MediatR;

namespace Klacks_api.Commands.Settings.Settings;

public record PostCommand(Models.Settings.Settings model) : IRequest<Models.Settings.Settings>;
