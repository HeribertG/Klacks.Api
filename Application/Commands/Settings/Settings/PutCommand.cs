using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Settings;

public record PutCommand(Klacks.Api.Domain.Models.Settings.Settings model) : IRequest<Klacks.Api.Domain.Models.Settings.Settings>;
