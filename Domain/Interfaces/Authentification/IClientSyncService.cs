// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs.IdentityProviders;
using Klacks.Api.Domain.DTOs.IdentityProviders;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IClientSyncService
{
    Task<IdentityProviderSyncResultResource> SyncClientsAsync(IdentityProvider provider);
}
