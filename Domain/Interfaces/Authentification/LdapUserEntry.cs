// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// One directory account as read from LDAP or Active Directory.
/// </summary>
public class LdapUserEntry
{
    public string DistinguishedName { get; set; } = string.Empty;
    public string? ObjectGuid { get; set; }
    public string? EntryUuid { get; set; }
    public string? Uid { get; set; }
    public string? CommonName { get; set; }
    public string? SamAccountName { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
    public string? DisplayName { get; set; }
    public string? Mail { get; set; }
    public string? TelephoneNumber { get; set; }
    public string? Mobile { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }
    public string? Company { get; set; }
    public string? PersonalTitle { get; set; }
    public string? StreetAddress { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public DateTime? WhenCreated { get; set; }
    public DateTime? WhenChanged { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The id that identifies this account across renames and moves: objectGUID (AD), else entryUUID (OpenLDAP),
    /// else the distinguished name, passed through NormalizeExternalId. Client import and directory login links both
    /// key on it.
    /// </summary>
    public string StableId => NormalizeExternalId(ObjectGuid ?? EntryUuid ?? DistinguishedName);

    /// <summary>
    /// The one normalization of a directory external id: a GUID becomes its lowercase hyphenated form, so ids typed or
    /// stored in another GUID format still match; any other id (a distinguished name) is returned trimmed.
    /// </summary>
    /// <param name="externalId">objectGUID, entryUUID or distinguished name</param>
    public static string NormalizeExternalId(string externalId) =>
        Guid.TryParse(externalId, out var guid) ? guid.ToString() : externalId.Trim();

    /// <summary>
    /// True when the id is a GUID (objectGUID or entryUUID), which the directory user cannot change; false for a
    /// distinguished-name fallback id.
    /// </summary>
    /// <param name="externalId">Normalized external id</param>
    public static bool IsGuidId(string externalId) => Guid.TryParse(externalId, out _);
}
