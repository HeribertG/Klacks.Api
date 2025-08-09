using MediatR;

namespace Klacks.Api.Application.Commands.Settings.States;

public record PutCommand(Models.Settings.State model) : IRequest<Models.Settings.State>;
