using Klacks.Api.Infrastructure.Mediator;
using S = Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Commands.Settings.Settings;

public record PostCommand(S.Settings model) : IRequest<S.Settings>;
