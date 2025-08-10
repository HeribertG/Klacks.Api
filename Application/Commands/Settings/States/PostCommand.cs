using MediatR;

namespace Klacks.Api.Application.Commands.Settings.States;

public record PostCommand(Klacks.Api.Domain.Models.Settings.State model) : IRequest<Klacks.Api.Domain.Models.Settings.State>;
