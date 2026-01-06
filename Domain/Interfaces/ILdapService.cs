using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Domain.Interfaces;

public interface ILdapService
{
    Task<TestConnectionResultResource> TestConnectionAsync(IdentityProvider provider);
    Task<List<LdapUserEntry>> GetUsersAsync(IdentityProvider provider);
    Task<bool> ValidateCredentialsAsync(IdentityProvider provider, string username, string password);
}

public class LdapUserEntry
{
    public string DistinguishedName { get; set; } = string.Empty;
    public string? ObjectGuid { get; set; }
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
    public string? StreetAddress { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public DateTime? WhenCreated { get; set; }
    public DateTime? WhenChanged { get; set; }
    public bool IsEnabled { get; set; } = true;
}
