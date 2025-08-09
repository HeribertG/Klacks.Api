using MediatR;

namespace Klacks.Api.Commands.Settings.States;

public record PutCommand(Models.Settings.State model) : IRequest<Models.Settings.State>;
