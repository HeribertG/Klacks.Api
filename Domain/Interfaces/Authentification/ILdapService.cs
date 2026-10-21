// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs.IdentityProviders;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface ILdapService
{
    Task<TestConnectionResultResource> TestConnectionAsync(IdentityProvider provider);
    Task<List<LdapUserEntry>> GetUsersAsync(IdentityProvider provider);
    Task<bool> ValidateCredentialsAsync(IdentityProvider provider, string username, string password);
}
