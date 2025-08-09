using MediatR;

namespace Klacks.Api.Commands.Settings.Settings;

public record PostCommand(Models.Settings.Settings model) : IRequest<Models.Settings.Settings>;
