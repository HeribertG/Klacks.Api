using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Commands.IdentityProviders;

public record PutCommand(IdentityProviderResource Model) : IRequest<IdentityProviderResource?>;
