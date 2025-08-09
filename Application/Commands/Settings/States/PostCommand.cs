using MediatR;

namespace Klacks.Api.Commands.Settings.States;

public record PostCommand(Models.Settings.State model) : IRequest<Models.Settings.State>;
