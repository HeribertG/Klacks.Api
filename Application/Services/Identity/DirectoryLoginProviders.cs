// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Services.Identity;

/// <summary>
/// Which identity providers take part in directory sign-in: LDAP and Active Directory providers marked for
/// authentication; Active Directory only over SSL (LDAPS), because AD refuses a simple user bind on an unencrypted
/// connection. The approval list deliberately does not require IsEnabled, so access can be prepared before the provider
/// is switched on; the sign-in path reads only enabled providers.
/// </summary>
public static class DirectoryLoginProviders
{
    /// <summary>
    /// True for LDAP and Active Directory.
    /// </summary>
    /// <param name="type">Provider type</param>
    public static bool IsDirectoryType(IdentityProviderType type) =>
        type is IdentityProviderType.Ldap or IdentityProviderType.ActiveDirectory;

    /// <summary>
    /// True when the provider is a directory provider marked for authentication, and, for Active Directory, uses SSL.
    /// </summary>
    /// <param name="provider">The provider</param>
    public static bool IsUsableForLogin(IdentityProvider provider) =>
        IsDirectoryType(provider.Type)
        && provider.UseForAuthentication
        && (provider.Type == IdentityProviderType.Ldap || provider.UseSsl);
}
