// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs.IdentityProviders;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IClientSyncService
{
    Task<IdentityProviderSyncResultResource> SyncClientsAsync(IdentityProvider provider);
}
