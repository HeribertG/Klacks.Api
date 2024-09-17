using MediatR;

namespace Klacks_api.Commands.Settings.Settings;

public record PutCommand(Models.Settings.Settings model) : IRequest<Models.Settings.Settings>;
