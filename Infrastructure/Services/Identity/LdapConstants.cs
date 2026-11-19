// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Identity;

/// <summary>
/// Fixed values of the LDAP client: ports, paging, timeouts, result codes, default filters and attribute names.
/// </summary>
public static class LdapConstants
{
    public const int DefaultPort = 389;
    public const int DefaultSslPort = 636;
    public const int ProtocolVersion = 3;
    public const int PageSize = 500;
    public const int TimeoutSeconds = 30;
    public const int LoginSearchSizeLimit = 2;
    public const int TestConnectionSampleNames = 5;
    public const int UserAccountControlDisabledBit = 0x2;
    public const int InvalidCredentialsErrorCode = 49;

    public const string DefaultActiveDirectoryUserFilter = "(&(objectCategory=person)(objectClass=user))";
    public const string DefaultLdapUserFilter = "(objectClass=person)";
    public const string ExcludeCriticalSystemObjectsFilter = "(!(isCriticalSystemObject=TRUE))";
    public const string GeneralizedTimeFormat = "yyyyMMddHHmmss.f'Z'";
    public const string GuidByteFormat = "x2";

    public const string DistinguishedName = "distinguishedName";
    public const string ObjectGuid = "objectGUID";
    public const string EntryUuid = "entryUUID";
    public const string Uid = "uid";
    public const string CommonName = "cn";
    public const string SamAccountName = "sAMAccountName";
    public const string UserPrincipalName = "userPrincipalName";
    public const string GivenName = "givenName";
    public const string Surname = "sn";
    public const string DisplayName = "displayName";
    public const string Mail = "mail";
    public const string TelephoneNumber = "telephoneNumber";
    public const string Mobile = "mobile";
    public const string Title = "title";
    public const string Department = "department";
    public const string Company = "company";
    public const string PersonalTitle = "personalTitle";
    public const string Street = "street";
    public const string StreetAddress = "streetAddress";
    public const string PostalCode = "postalCode";
    public const string Locality = "l";
    public const string StateOrProvince = "st";
    public const string Country = "c";
    public const string WhenCreated = "whenCreated";
    public const string WhenChanged = "whenChanged";
    public const string UserAccountControl = "userAccountControl";

    public static readonly string[] UserAttributes =
    [
        DistinguishedName, ObjectGuid, EntryUuid, Uid, CommonName, SamAccountName, UserPrincipalName,
        GivenName, Surname, DisplayName, Mail, TelephoneNumber, Mobile,
        Title, Department, Company, PersonalTitle, Street, StreetAddress, PostalCode,
        Locality, StateOrProvince, Country, WhenCreated, WhenChanged, UserAccountControl,
    ];
}
