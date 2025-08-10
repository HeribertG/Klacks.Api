using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Settings;

public record PostCommand(Klacks.Api.Domain.Models.Settings.Settings model) : IRequest<Klacks.Api.Domain.Models.Settings.Settings>;
