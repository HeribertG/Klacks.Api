using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Commands.IdentityProviders;

public record PostCommand(IdentityProviderResource Model) : IRequest<IdentityProviderResource?>;
