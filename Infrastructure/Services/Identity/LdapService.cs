using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Infrastructure.Services.Identity;

public class LdapService : ILdapService
{
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
                using var connection = CreateConnection(provider);
                BindWithFallback(connection, provider);

                var users = SearchUsers(connection, provider, 10);

                return new TestConnectionResultResource
                {
                    Success = true,
                    UserCount = users.Count,
                    SampleUsers = users.Take(5).Select(u => u.DisplayName ?? u.SamAccountName ?? u.DistinguishedName).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LDAP connection test failed for provider {Name}", provider.Name);
                return new TestConnectionResultResource
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    public async Task<List<LdapUserEntry>> GetUsersAsync(IdentityProvider provider)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var connection = CreateConnection(provider);
                BindWithFallback(connection, provider);

                return SearchUsers(connection, provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users from LDAP provider {Name}", provider.Name);
                throw;
            }
        });
    }

    public async Task<bool> ValidateCredentialsAsync(IdentityProvider provider, string username, string password)
    {
        return await Task.Run(() =>
        {
            try
            {
                var host = provider.Host ?? throw new InvalidOperationException("LDAP host not configured");
                var port = provider.Port ?? (provider.UseSsl ? 636 : 389);
                var baseDn = provider.BaseDn ?? string.Empty;

                var bindDn = BuildUserDn(username, baseDn, provider.Type);
                _logger.LogDebug("LDAP auth attempt for {Username} via {Provider} ({Host}:{Port})", username, provider.Name, host, port);

                var identifier = new LdapDirectoryIdentifier(host, port);
                var credential = new NetworkCredential(bindDn, password);

                using var connection = new LdapConnection(identifier, credential, AuthType.Basic);
                connection.SessionOptions.ProtocolVersion = 3;

                if (provider.UseSsl)
                {
                    connection.SessionOptions.SecureSocketLayer = true;
                }

                connection.Bind();
                _logger.LogInformation("LDAP authentication successful for user {Username} via {Provider}", username, provider.Name);
                return true;
            }
            catch (LdapException ex)
            {
                _logger.LogDebug("LDAP authentication failed for user {Username}: {Message}", username, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LDAP authentication error for user {Username}", username);
                return false;
            }
        });
    }

    private static string BuildUserDn(string username, string baseDn, IdentityProviderType providerType)
    {
        if (username.Contains('=') && username.Contains(','))
        {
            return username;
        }

        var usernameOnly = username.Contains('@') ? username.Split('@')[0] : username;

        if (providerType == IdentityProviderType.ActiveDirectory)
        {
            return $"cn={usernameOnly},{baseDn}";
        }

        return $"uid={usernameOnly},{baseDn}";
    }

    private LdapConnection CreateConnection(IdentityProvider provider, AuthType? authTypeOverride = null)
    {
        var host = provider.Host ?? throw new InvalidOperationException("LDAP host not configured");
        var port = provider.Port ?? (provider.UseSsl ? 636 : 389);

        var identifier = new LdapDirectoryIdentifier(host, port);

        LdapConnection connection;

        if (!string.IsNullOrEmpty(provider.BindDn) && !string.IsNullOrEmpty(provider.BindPassword))
        {
            var credential = new NetworkCredential(provider.BindDn, provider.BindPassword);
            connection = new LdapConnection(identifier, credential);
        }
        else
        {
            connection = new LdapConnection(identifier);
        }

        connection.SessionOptions.ProtocolVersion = 3;

        if (provider.UseSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }

        if (authTypeOverride.HasValue)
        {
            connection.AuthType = authTypeOverride.Value;
        }
        else if (provider.Type == IdentityProviderType.ActiveDirectory)
        {
            connection.AuthType = AuthType.Negotiate;
        }
        else
        {
            connection.AuthType = AuthType.Basic;
        }

        return connection;
    }

    private void BindWithFallback(LdapConnection connection, IdentityProvider provider)
    {
        try
        {
            connection.Bind();
        }
        catch (Exception ex) when (provider.Type == IdentityProviderType.ActiveDirectory &&
            (ex is LdapException || ex is DirectoryOperationException))
        {
            _logger.LogDebug("Negotiate auth failed for {Provider}, falling back to Basic auth: {Message}", provider.Name, ex.Message);
            connection.AuthType = AuthType.Basic;
            connection.Bind();
        }
    }

    private List<LdapUserEntry> SearchUsers(LdapConnection connection, IdentityProvider provider, int? limit = null)
    {
        var baseDn = provider.BaseDn ?? throw new InvalidOperationException("LDAP Base DN not configured");
        var filter = provider.UserFilter ?? "(objectClass=person)";

        var attributes = new[]
        {
            "distinguishedName", "objectGUID", "sAMAccountName", "userPrincipalName",
            "givenName", "sn", "displayName", "mail", "telephoneNumber", "mobile",
            "title", "department", "company", "personalTitle", "street", "streetAddress", "postalCode",
            "l", "st", "c", "whenCreated", "whenChanged", "userAccountControl"
        };

        var searchRequest = new SearchRequest(
            baseDn,
            filter,
            SearchScope.Subtree,
            attributes);

        searchRequest.SizeLimit = limit ?? 500;

        var users = new List<LdapUserEntry>();

        try
        {
            var response = (SearchResponse)connection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in response.Entries)
            {
                users.Add(MapToLdapUserEntry(entry));
            }
        }
        catch (DirectoryOperationException ex) when (ex.Response is SearchResponse partialResponse)
        {
            _logger.LogWarning("LDAP search returned partial results due to size limit");
            foreach (SearchResultEntry entry in partialResponse.Entries)
            {
                users.Add(MapToLdapUserEntry(entry));
            }
        }

        return users;
    }

    private static LdapUserEntry MapToLdapUserEntry(SearchResultEntry entry)
    {
        var user = new LdapUserEntry
        {
            DistinguishedName = entry.DistinguishedName,
            ObjectGuid = GetGuidAttribute(entry, "objectGUID"),
            SamAccountName = GetStringAttribute(entry, "sAMAccountName"),
            UserPrincipalName = GetStringAttribute(entry, "userPrincipalName"),
            GivenName = GetStringAttribute(entry, "givenName"),
            Surname = GetStringAttribute(entry, "sn"),
            DisplayName = GetStringAttribute(entry, "displayName"),
            Mail = GetStringAttribute(entry, "mail"),
            TelephoneNumber = GetStringAttribute(entry, "telephoneNumber"),
            Mobile = GetStringAttribute(entry, "mobile"),
            Title = GetStringAttribute(entry, "title"),
            Department = GetStringAttribute(entry, "department"),
            Company = GetStringAttribute(entry, "company"),
            PersonalTitle = GetStringAttribute(entry, "personalTitle"),
            StreetAddress = GetStringAttribute(entry, "streetAddress") ?? GetStringAttribute(entry, "street"),
            PostalCode = GetStringAttribute(entry, "postalCode"),
            City = GetStringAttribute(entry, "l"),
            State = GetStringAttribute(entry, "st"),
            Country = GetStringAttribute(entry, "c"),
            WhenCreated = GetDateTimeAttribute(entry, "whenCreated"),
            WhenChanged = GetDateTimeAttribute(entry, "whenChanged")
        };

        var uacValue = GetIntAttribute(entry, "userAccountControl");
        if (uacValue.HasValue)
        {
            user.IsEnabled = (uacValue.Value & 0x2) == 0;
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
        if (int.TryParse(strValue, out var result))
        {
            return result;
        }
        return null;
    }

    private static DateTime? GetDateTimeAttribute(SearchResultEntry entry, string attributeName)
    {
        var strValue = GetStringAttribute(entry, attributeName);
        if (!string.IsNullOrEmpty(strValue))
        {
            if (DateTime.TryParseExact(strValue, "yyyyMMddHHmmss.f'Z'", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var result))
            {
                return result;
            }
        }
        return null;
    }
}
