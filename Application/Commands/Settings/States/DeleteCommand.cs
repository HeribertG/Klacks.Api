using MediatR;

namespace Klacks.Api.Application.Commands.Settings.States;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.State>;
