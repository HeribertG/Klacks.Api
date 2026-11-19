// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Services.Identity;

/// <summary>
/// The one place that derives display name, login name, e-mail and the names of a created account from a directory
/// entry, and builds the approval list rows from an entry or from a linked account the directory no longer returns.
/// </summary>
public static class DirectoryAccountResourceFactory
{
    private const char NameSeparator = ' ';

    /// <summary>
    /// displayName, else "givenName sn", else cn, else the login name.
    /// </summary>
    /// <param name="entry">Directory entry</param>
    /// <param name="type">Provider type, decides the login attribute</param>
    public static string DisplayName(LdapUserEntry entry, IdentityProviderType type)
    {
        if (!string.IsNullOrWhiteSpace(entry.DisplayName))
        {
            return entry.DisplayName.Trim();
        }

        var fullName = JoinNames(entry.GivenName, entry.Surname);
        if (fullName.Length > 0)
        {
            return fullName;
        }

        if (!string.IsNullOrWhiteSpace(entry.CommonName))
        {
            return entry.CommonName.Trim();
        }

        var loginName = LoginName(entry, type);
        return loginName.Length > 0 ? loginName : entry.DistinguishedName;
    }

    /// <summary>
    /// The name typed into the login form, always one that LdapUserFilters.ForLogin matches: AD sAMAccountName, else
    /// userPrincipalName, else mail; LDAP uid, else mail; empty when none is set.
    /// </summary>
    /// <param name="entry">Directory entry</param>
    /// <param name="type">Provider type</param>
    public static string LoginName(LdapUserEntry entry, IdentityProviderType type)
    {
        var candidate = type == IdentityProviderType.ActiveDirectory
            ? FirstNonBlank(entry.SamAccountName, entry.UserPrincipalName, entry.Mail)
            : FirstNonBlank(entry.Uid, entry.Mail);

        return candidate ?? string.Empty;
    }

    /// <summary>
    /// The e-mail of an account created or linked from this entry: mail, else userPrincipalName, each only when it is
    /// one well-formed address; null when neither is.
    /// </summary>
    /// <param name="entry">Directory entry</param>
    public static string? Email(LdapUserEntry entry)
    {
        if (EmailAddressFormat.TryNormalize(entry.Mail, out var mail))
        {
            return mail;
        }

        return EmailAddressFormat.TryNormalize(entry.UserPrincipalName, out var principalName) ? principalName : null;
    }

    /// <summary>
    /// Last name of a created account: sn, else displayName, else the display name.
    /// </summary>
    /// <param name="entry">Directory entry</param>
    /// <param name="type">Provider type</param>
    public static string LastName(LdapUserEntry entry, IdentityProviderType type) =>
        FirstNonBlank(entry.Surname, entry.DisplayName) ?? DisplayName(entry, type);

    /// <summary>
    /// First name of a created account: givenName, else empty.
    /// </summary>
    /// <param name="entry">Directory entry</param>
    public static string FirstName(LdapUserEntry entry) => entry.GivenName?.Trim() ?? string.Empty;

    /// <summary>
    /// Row for an entry the directory returned.
    /// </summary>
    /// <param name="entry">Directory entry</param>
    /// <param name="type">Provider type</param>
    /// <param name="linkedUser">Account linked to the entry, or null</param>
    /// <param name="emailInUse">True when the entry's e-mail is already used by an account; ignored when linkedUser is set</param>
    /// <param name="hasRoles">True when the linked account holds a role; ignored without linkedUser</param>
    public static DirectoryAccountResource FromEntry(
        LdapUserEntry entry, IdentityProviderType type, AppUser? linkedUser, bool emailInUse, bool hasRoles)
    {
        return new DirectoryAccountResource
        {
            ExternalId = entry.StableId,
            DisplayName = DisplayName(entry, type),
            LoginName = LoginName(entry, type),
            Email = Email(entry),
            MayLogin = linkedUser is { DeactivatedAt: null },
            UserId = linkedUser?.Id,
            EmailInUse = linkedUser == null && emailInUse,
            HasRoles = linkedUser != null && hasRoles,
            IsInDirectory = true,
        };
    }

    /// <summary>
    /// Row for a linked account the directory did not return; its directory login name is unknown, so it stays empty.
    /// </summary>
    /// <param name="user">Linked account; its DirectoryExternalId becomes the row key</param>
    /// <param name="hasRoles">True when the account holds a role</param>
    public static DirectoryAccountResource FromLinkedUser(AppUser user, bool hasRoles)
    {
        var fullName = JoinNames(user.FirstName, user.LastName);

        return new DirectoryAccountResource
        {
            ExternalId = user.DirectoryExternalId ?? string.Empty,
            DisplayName = fullName.Length > 0 ? fullName : user.UserName ?? string.Empty,
            LoginName = string.Empty,
            Email = user.Email,
            MayLogin = user.DeactivatedAt == null,
            UserId = user.Id,
            EmailInUse = false,
            HasRoles = hasRoles,
            IsInDirectory = false,
        };
    }

    private static string JoinNames(string? first, string? last) =>
        string.Join(NameSeparator, new[] { first, last }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
