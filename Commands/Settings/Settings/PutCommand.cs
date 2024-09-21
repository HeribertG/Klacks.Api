using MediatR;

namespace Klacks.Api.Commands.Settings.Settings;

public record PutCommand(Models.Settings.Settings model) : IRequest<Models.Settings.Settings>;
