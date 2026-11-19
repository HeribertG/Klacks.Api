// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Net;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs.IdentityProviders;

namespace Klacks.Api.Infrastructure.Services.Identity;

/// <summary>
/// LDAP / Active Directory client: reads accounts with the provider's service account (paged, referrals not chased)
/// and checks a password by binding as the user's own DN. The authentication type is fixed per provider: with SSL a
/// simple bind over TLS (both types); Active Directory without SSL Negotiate, read-only (no user bind); LDAP without SSL
/// a simple bind. Directory errors of the read methods surface as DirectoryUnavailableException; the connection test
/// answers a fixed message instead of the directory's error text.
/// </summary>
/// <param name="logger">Records directory failures without credentials</param>
public class LdapService : ILdapService
{
    private const string HostNotConfiguredMessage = "LDAP host not configured";
    private const string BaseDnNotConfiguredMessage = "LDAP Base DN not configured";
    private const string ConnectionTestFailedMessage =
        "The connection to the directory failed. Check host, port, SSL and the bind account.";
    private const string ActiveDirectoryUserBindRequiresSslMessage =
        "Signing in through Active Directory requires an encrypted connection (LDAPS).";

    private static readonly ResultCode[] PartialResultCodes =
        [ResultCode.SizeLimitExceeded, ResultCode.AdminLimitExceeded, ResultCode.TimeLimitExceeded];

    private readonly ILogger<LdapService> _logger;

    public LdapService(ILogger<LdapService> logger)
    {
        _logger = logger;
    }

    public async Task<TestConnectionResultResource> TestConnectionAsync(IdentityProvider provider)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var connection = CreateServiceConnection(provider);
                var result = SearchPaged(
                    connection, RequireBaseDn(provider), LdapUserFilters.Base(provider), null, skipDisabled: true);

                return new TestConnectionResultResource
                {
                    Success = true,
                    UserCount = result.Entries.Count,
                    IsTruncated = !result.IsComplete,
                    SampleUsers = result.Entries
                        .Take(LdapConstants.TestConnectionSampleNames)
                        .Select(u => u.DisplayName ?? u.SamAccountName ?? u.Uid ?? u.CommonName ?? u.DistinguishedName)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LDAP connection test failed for provider {ProviderId}", provider.Id);
                return new TestConnectionResultResource
                {
                    Success = false,
                    ErrorMessage = ConnectionTestFailedMessage
                };
            }
        });
    }

    public Task<LdapUserSearchResult> GetUsersAsync(IdentityProvider provider)
    {
        return RunDirectoryReadAsync(provider, connection =>
            SearchPaged(connection, RequireBaseDn(provider), LdapUserFilters.Base(provider), null, skipDisabled: false));
    }

    public Task<LdapUserSearchResult> SearchAccountsAsync(IdentityProvider provider, string? searchTerm, int maxEntries)
    {
        return RunDirectoryReadAsync(provider, connection =>
            SearchPaged(
                connection, RequireBaseDn(provider), LdapUserFilters.ForListing(provider, searchTerm),
                maxEntries, skipDisabled: true));
    }

    public async Task<LdapUserEntry?> FindLoginEntryAsync(IdentityProvider provider, string loginName)
    {
        if (string.IsNullOrWhiteSpace(loginName))
        {
            return null;
        }

        return await RunDirectoryReadAsync(provider, connection =>
        {
            var request = new SearchRequest(
                RequireBaseDn(provider),
                LdapUserFilters.ForLogin(provider, loginName.Trim()),
                SearchScope.Subtree,
                LdapConstants.UserAttributes)
            {
                SizeLimit = LdapConstants.LoginSearchSizeLimit
            };

            SearchResponse response;
            try
            {
                response = (SearchResponse)connection.SendRequest(request);
            }
            catch (DirectoryOperationException ex) when (ex.Response is SearchResponse { ResultCode: ResultCode.SizeLimitExceeded })
            {
                return null;
            }

            return SingleEnabled(response);
        });
    }

    public async Task<LdapUserEntry?> FindByExternalIdAsync(IdentityProvider provider, string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        var baseDn = RequireBaseDn(provider);
        var isDistinguishedName = LdapUserFilters.IsDistinguishedNameId(externalId);
        if (isDistinguishedName && !externalId.EndsWith(baseDn, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return await RunDirectoryReadAsync(provider, connection =>
        {
            var request = new SearchRequest(
                isDistinguishedName ? externalId : baseDn,
                LdapUserFilters.ForExternalId(provider, externalId),
                isDistinguishedName ? SearchScope.Base : SearchScope.Subtree,
                LdapConstants.UserAttributes);

            SearchResponse response;
            try
            {
                response = (SearchResponse)connection.SendRequest(request);
            }
            catch (DirectoryOperationException ex) when (ex.Response is SearchResponse { ResultCode: ResultCode.NoSuchObject })
            {
                return null;
            }

            return SingleEnabled(response);
        });
    }

    public async Task<bool> BindAsUserAsync(IdentityProvider provider, string distinguishedName, string password)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(distinguishedName))
        {
            return false;
        }

        if (provider.Type == IdentityProviderType.ActiveDirectory && !provider.UseSsl)
        {
            throw new DirectoryUnavailableException(new InvalidOperationException(ActiveDirectoryUserBindRequiresSslMessage));
        }

        return await Task.Run(() =>
        {
            var identifier = new LdapDirectoryIdentifier(RequireHost(provider), ResolvePort(provider));
            using var connection = new LdapConnection(
                identifier, new NetworkCredential(distinguishedName, password), AuthType.Basic);
            ApplySessionOptions(connection, provider);

            try
            {
                connection.Bind();
                return true;
            }
            catch (LdapException ex) when (ex.ErrorCode == LdapConstants.InvalidCredentialsErrorCode)
            {
                return false;
            }
            catch (DirectoryException ex)
            {
                _logger.LogWarning(ex, "LDAP user bind failed with a directory error via provider {ProviderId}", provider.Id);
                throw new DirectoryUnavailableException(ex);
            }
        });
    }

    private Task<T> RunDirectoryReadAsync<T>(IdentityProvider provider, Func<LdapConnection, T> read)
    {
        return Task.Run(() =>
        {
            try
            {
                using var connection = CreateServiceConnection(provider);
                return read(connection);
            }
            catch (DirectoryException ex)
            {
                _logger.LogWarning(ex, "LDAP read failed for provider {ProviderId}", provider.Id);
                throw new DirectoryUnavailableException(ex);
            }
        });
    }

    private LdapConnection CreateServiceConnection(IdentityProvider provider)
    {
        var identifier = new LdapDirectoryIdentifier(RequireHost(provider), ResolvePort(provider));

        var connection = !string.IsNullOrEmpty(provider.BindDn) && !string.IsNullOrEmpty(provider.BindPassword)
            ? new LdapConnection(identifier, new NetworkCredential(provider.BindDn, provider.BindPassword))
            : new LdapConnection(identifier);

        ApplySessionOptions(connection, provider);
        connection.AuthType = ServiceAuthType(provider);

        try
        {
            connection.Bind();
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return connection;
    }

    private static void ApplySessionOptions(LdapConnection connection, IdentityProvider provider)
    {
        connection.SessionOptions.ProtocolVersion = LdapConstants.ProtocolVersion;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        connection.Timeout = TimeSpan.FromSeconds(LdapConstants.TimeoutSeconds);

        if (provider.UseSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }
    }

    /// <summary>
    /// SSL: simple bind over TLS for both types. Active Directory without SSL: Negotiate (Kerberos/NTLM signing), which
    /// AD accepts on 389 where it refuses a simple bind. LDAP without SSL: simple bind.
    /// </summary>
    internal static AuthType ServiceAuthType(IdentityProvider provider) =>
        !provider.UseSsl && provider.Type == IdentityProviderType.ActiveDirectory ? AuthType.Negotiate : AuthType.Basic;

    private static string RequireHost(IdentityProvider provider) =>
        provider.Host ?? throw new InvalidOperationException(HostNotConfiguredMessage);

    private static string RequireBaseDn(IdentityProvider provider) =>
        provider.BaseDn ?? throw new InvalidOperationException(BaseDnNotConfiguredMessage);

    private static int ResolvePort(IdentityProvider provider) =>
        provider.Port ?? (provider.UseSsl ? LdapConstants.DefaultSslPort : LdapConstants.DefaultPort);

    private static LdapUserSearchResult SearchPaged(
        LdapConnection connection, string baseDn, string filter, int? maxEntries, bool skipDisabled)
    {
        var entries = new List<LdapUserEntry>();
        var pageControl = new PageResultRequestControl(LdapConstants.PageSize) { IsCritical = false };
        var request = new SearchRequest(baseDn, filter, SearchScope.Subtree, LdapConstants.UserAttributes)
        {
            SizeLimit = 0
        };
        request.Controls.Add(pageControl);

        while (true)
        {
            SearchResponse response;
            var pageComplete = true;
            try
            {
                response = (SearchResponse)connection.SendRequest(request);
            }
            catch (DirectoryOperationException ex) when (ex.Response is SearchResponse partial
                && PartialResultCodes.Contains(partial.ResultCode))
            {
                response = partial;
                pageComplete = false;
            }

            if (!Collect(response, entries, maxEntries, skipDisabled))
            {
                return new LdapUserSearchResult(entries, false);
            }

            if (!pageComplete || response.ResultCode != ResultCode.Success)
            {
                return new LdapUserSearchResult(entries, false);
            }

            var cookie = response.Controls.OfType<PageResultResponseControl>().FirstOrDefault()?.Cookie;
            if (cookie is null || cookie.Length == 0)
            {
                return new LdapUserSearchResult(entries, true);
            }

            pageControl.Cookie = cookie;
        }
    }

    private static bool Collect(SearchResponse response, List<LdapUserEntry> entries, int? maxEntries, bool skipDisabled)
    {
        foreach (SearchResultEntry entry in response.Entries)
        {
            var user = MapToLdapUserEntry(entry);
            if (skipDisabled && !user.IsEnabled)
            {
                continue;
            }

            if (maxEntries.HasValue && entries.Count >= maxEntries.Value)
            {
                return false;
            }

            entries.Add(user);
        }

        return true;
    }

    private static LdapUserEntry? SingleEnabled(SearchResponse response)
    {
        var enabled = response.Entries
            .Cast<SearchResultEntry>()
            .Select(MapToLdapUserEntry)
            .Where(entry => entry.IsEnabled)
            .ToList();

        return enabled.Count == 1 ? enabled[0] : null;
    }

    private static LdapUserEntry MapToLdapUserEntry(SearchResultEntry entry)
    {
        var user = new LdapUserEntry
        {
            DistinguishedName = entry.DistinguishedName,
            ObjectGuid = GetGuidAttribute(entry, LdapConstants.ObjectGuid),
            EntryUuid = GetStringAttribute(entry, LdapConstants.EntryUuid),
            Uid = GetStringAttribute(entry, LdapConstants.Uid),
            CommonName = GetStringAttribute(entry, LdapConstants.CommonName),
            SamAccountName = GetStringAttribute(entry, LdapConstants.SamAccountName),
            UserPrincipalName = GetStringAttribute(entry, LdapConstants.UserPrincipalName),
            GivenName = GetStringAttribute(entry, LdapConstants.GivenName),
            Surname = GetStringAttribute(entry, LdapConstants.Surname),
            DisplayName = GetStringAttribute(entry, LdapConstants.DisplayName),
            Mail = GetStringAttribute(entry, LdapConstants.Mail),
            TelephoneNumber = GetStringAttribute(entry, LdapConstants.TelephoneNumber),
            Mobile = GetStringAttribute(entry, LdapConstants.Mobile),
            Title = GetStringAttribute(entry, LdapConstants.Title),
            Department = GetStringAttribute(entry, LdapConstants.Department),
            Company = GetStringAttribute(entry, LdapConstants.Company),
            PersonalTitle = GetStringAttribute(entry, LdapConstants.PersonalTitle),
            StreetAddress = GetStringAttribute(entry, LdapConstants.StreetAddress) ?? GetStringAttribute(entry, LdapConstants.Street),
            PostalCode = GetStringAttribute(entry, LdapConstants.PostalCode),
            City = GetStringAttribute(entry, LdapConstants.Locality),
            State = GetStringAttribute(entry, LdapConstants.StateOrProvince),
            Country = GetStringAttribute(entry, LdapConstants.Country),
            WhenCreated = GetDateTimeAttribute(entry, LdapConstants.WhenCreated),
            WhenChanged = GetDateTimeAttribute(entry, LdapConstants.WhenChanged)
        };

        var uacValue = GetIntAttribute(entry, LdapConstants.UserAccountControl);
        if (uacValue.HasValue)
        {
            user.IsEnabled = (uacValue.Value & LdapConstants.UserAccountControlDisabledBit) == 0;
        }

        return user;
    }

    private static string? GetStringAttribute(SearchResultEntry entry, string attributeName)
    {
        if (entry.Attributes.Contains(attributeName))
        {
            var values = entry.Attributes[attributeName].GetValues(typeof(string));
            if (values.Length > 0)
            {
                return values[0] as string;
            }
        }
        return null;
    }

    private static string? GetGuidAttribute(SearchResultEntry entry, string attributeName)
    {
        if (entry.Attributes.Contains(attributeName))
        {
            var values = entry.Attributes[attributeName].GetValues(typeof(byte[]));
            if (values.Length > 0 && values[0] is byte[] bytes)
            {
                return new Guid(bytes).ToString();
            }
        }
        return null;
    }

    private static int? GetIntAttribute(SearchResultEntry entry, string attributeName)
    {
        var strValue = GetStringAttribute(entry, attributeName);
        if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return null;
    }

    private static DateTime? GetDateTimeAttribute(SearchResultEntry entry, string attributeName)
    {
        var strValue = GetStringAttribute(entry, attributeName);
        if (!string.IsNullOrEmpty(strValue)
            && DateTime.TryParseExact(
                strValue, LdapConstants.GeneralizedTimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }
        return null;
    }
}
