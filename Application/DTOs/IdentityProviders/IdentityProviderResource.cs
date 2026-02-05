using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.IdentityProviders;

public class IdentityProviderResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IdentityProviderType Type { get; set; }

    public bool IsEnabled { get; set; }

    public int SortOrder { get; set; }

    public bool UseForAuthentication { get; set; }

    public bool UseForClientImport { get; set; }

    public string? Host { get; set; }

    public int? Port { get; set; }

    public bool UseSsl { get; set; }

    public string? BaseDn { get; set; }

    public string? BindDn { get; set; }

    public string? BindPassword { get; set; }

    public string? UserFilter { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AuthorizationUrl { get; set; }

    public string? TokenUrl { get; set; }

    public string? UserInfoUrl { get; set; }

    public string? Scopes { get; set; }

    public string? TenantId { get; set; }

    public DateTime? LastSyncTime { get; set; }

    public int? LastSyncCount { get; set; }

    public string? LastSyncError { get; set; }

    public Dictionary<string, string>? AttributeMapping { get; set; }
}
