using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Commands.IdentityProviders;

public record SyncClientsCommand(Guid Id) : IRequest<IdentityProviderSyncResultResource>;
