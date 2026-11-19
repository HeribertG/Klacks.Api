// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Infrastructure.Services.Identity;

/// <summary>
/// Builds the LDAP search filters of the directory login and approval list. Every typed value passes through
/// LdapFilterEncoder; the provider's own UserFilter (or the type default) restricts each filter.
/// </summary>
public static class LdapUserFilters
{
    private const char FilterOpen = '(';
    private const char ValueEscape = '\\';
    private const string Wildcard = "*";

    private static readonly string[] ActiveDirectoryLoginAttributes =
        [LdapConstants.SamAccountName, LdapConstants.UserPrincipalName, LdapConstants.Mail];

    private static readonly string[] LdapLoginAttributes = [LdapConstants.Uid, LdapConstants.Mail];

    private static readonly string[] ActiveDirectoryListingAttributes =
    [
        LdapConstants.CommonName, LdapConstants.DisplayName, LdapConstants.Mail,
        LdapConstants.SamAccountName, LdapConstants.Surname, LdapConstants.GivenName,
    ];

    private static readonly string[] LdapListingAttributes =
    [
        LdapConstants.CommonName, LdapConstants.DisplayName, LdapConstants.Mail,
        LdapConstants.Uid, LdapConstants.Surname, LdapConstants.GivenName,
    ];

    /// <summary>
    /// The provider's UserFilter, wrapped in parentheses when it has none, or the default of the provider type. For
    /// Active Directory the system accounts (isCriticalSystemObject: Administrator, Guest, krbtgt) are always excluded.
    /// </summary>
    /// <param name="provider">Directory provider</param>
    public static string Base(IdentityProvider provider)
    {
        var isActiveDirectory = provider.Type == IdentityProviderType.ActiveDirectory;
        var configured = provider.UserFilter?.Trim();
        string filter;
        if (string.IsNullOrEmpty(configured))
        {
            filter = isActiveDirectory ? LdapConstants.DefaultActiveDirectoryUserFilter : LdapConstants.DefaultLdapUserFilter;
        }
        else
        {
            filter = configured.StartsWith(FilterOpen) ? configured : $"({configured})";
        }

        return isActiveDirectory ? $"(&{filter}{LdapConstants.ExcludeCriticalSystemObjectsFilter})" : filter;
    }

    /// <summary>
    /// Matches the typed login name exactly against the login attributes of the provider type
    /// (AD: sAMAccountName, userPrincipalName, mail; LDAP: uid, mail).
    /// </summary>
    /// <param name="provider">Directory provider</param>
    /// <param name="loginName">Text typed into the login form</param>
    public static string ForLogin(IdentityProvider provider, string loginName)
    {
        var attributes = provider.Type == IdentityProviderType.ActiveDirectory
            ? ActiveDirectoryLoginAttributes
            : LdapLoginAttributes;

        return Combine(provider, attributes, LdapFilterEncoder.Escape(loginName));
    }

    /// <summary>
    /// Matches the search term as a substring of the name, login and e-mail attributes; a blank term returns Base.
    /// </summary>
    /// <param name="provider">Directory provider</param>
    /// <param name="searchTerm">Substring typed into the approval list search box</param>
    public static string ForListing(IdentityProvider provider, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Base(provider);
        }

        var attributes = provider.Type == IdentityProviderType.ActiveDirectory
            ? ActiveDirectoryListingAttributes
            : LdapListingAttributes;

        return Combine(provider, attributes, Wildcard + LdapFilterEncoder.Escape(searchTerm.Trim()) + Wildcard);
    }

    /// <summary>
    /// Matches one account by its stable id: AD objectGUID as escaped bytes of Guid.ToByteArray(), LDAP entryUUID.
    /// An id that is not a GUID is a distinguished name; then the filter is Base and the caller searches with base
    /// scope on that DN (see IsDistinguishedNameId).
    /// </summary>
    /// <param name="provider">Directory provider</param>
    /// <param name="externalId">LdapUserEntry.StableId</param>
    public static string ForExternalId(IdentityProvider provider, string externalId)
    {
        if (!Guid.TryParse(externalId, out var guid))
        {
            return Base(provider);
        }

        var match = provider.Type == IdentityProviderType.ActiveDirectory
            ? $"({LdapConstants.ObjectGuid}={EncodeBytes(guid.ToByteArray())})"
            : $"({LdapConstants.EntryUuid}={LdapFilterEncoder.Escape(guid.ToString())})";

        return $"(&{Base(provider)}{match})";
    }

    /// <summary>
    /// True when the stable id is a distinguished name rather than a GUID.
    /// </summary>
    /// <param name="externalId">LdapUserEntry.StableId</param>
    public static bool IsDistinguishedNameId(string externalId) => !LdapUserEntry.IsGuidId(externalId);

    private static string Combine(IdentityProvider provider, string[] attributes, string encodedValue)
    {
        var alternatives = new StringBuilder();
        foreach (var attribute in attributes)
        {
            alternatives.Append($"({attribute}={encodedValue})");
        }

        return $"(&{Base(provider)}(|{alternatives}))";
    }

    private static string EncodeBytes(byte[] bytes)
    {
        var builder = new StringBuilder();
        foreach (var value in bytes)
        {
            builder.Append(ValueEscape).Append(value.ToString(LdapConstants.GuidByteFormat));
        }

        return builder.ToString();
    }
}
