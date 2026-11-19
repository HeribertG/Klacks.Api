// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs.IdentityProviders;

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Reads an LDAP or Active Directory server with the provider's service account and checks a user's password by
/// binding as that user. The search methods throw DirectoryUnavailableException when the directory cannot be read.
/// </summary>
public interface ILdapService
{
    Task<TestConnectionResultResource> TestConnectionAsync(IdentityProvider provider);

    /// <summary>
    /// Every entry under the base DN that matches the user filter, disabled accounts included, read page by page.
    /// </summary>
    /// <param name="provider">Directory provider with host, base DN, bind account and user filter</param>
    Task<LdapUserSearchResult> GetUsersAsync(IdentityProvider provider);

    /// <summary>
    /// Enabled accounts whose names, login name or e-mail contain the search term, at most maxEntries of them.
    /// </summary>
    /// <param name="provider">Directory provider</param>
    /// <param name="searchTerm">Substring to look for; null or blank lists every account</param>
    /// <param name="maxEntries">Cap on the returned entries; reaching it marks the result incomplete</param>
    Task<LdapUserSearchResult> SearchAccountsAsync(IdentityProvider provider, string? searchTerm, int maxEntries);

    /// <summary>
    /// The one enabled account whose login name (AD: sAMAccountName, userPrincipalName, mail; LDAP: uid, mail)
    /// equals the typed text; null when none or more than one matches.
    /// </summary>
    /// <param name="provider">Directory provider</param>
    /// <param name="loginName">Text the user typed into the login form</param>
    Task<LdapUserEntry?> FindLoginEntryAsync(IdentityProvider provider, string loginName);

    /// <summary>
    /// The enabled account with this stable id (objectGUID, entryUUID or distinguished name); null when not found.
    /// </summary>
    /// <param name="provider">Directory provider</param>
    /// <param name="externalId">LdapUserEntry.StableId of the account</param>
    Task<LdapUserEntry?> FindByExternalIdAsync(IdentityProvider provider, string externalId);

    /// <summary>
    /// Binds as the given entry with the given password. False for a blank password or invalid credentials; any
    /// other directory error throws DirectoryUnavailableException.
    /// </summary>
    /// <param name="provider">Directory provider</param>
    /// <param name="distinguishedName">DN of the entry found by FindLoginEntryAsync</param>
    /// <param name="password">Password the user typed</param>
    Task<bool> BindAsUserAsync(IdentityProvider provider, string distinguishedName, string password);
}
