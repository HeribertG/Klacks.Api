using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Authentification;

public class IdentityProvider : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public IdentityProviderType Type { get; set; }

    public bool IsEnabled { get; set; }

    public int SortOrder { get; set; }

    public bool UseForAuthentication { get; set; }

    public bool UseForClientImport { get; set; }

    [MaxLength(255)]
    public string? Host { get; set; }

    public int? Port { get; set; }

    public bool UseSsl { get; set; }

    [MaxLength(500)]
    public string? BaseDn { get; set; }

    [MaxLength(500)]
    public string? BindDn { get; set; }

    [MaxLength(500)]
    public string? BindPassword { get; set; }

    [MaxLength(500)]
    public string? UserFilter { get; set; }

    [MaxLength(500)]
    public string? ClientId { get; set; }

    [MaxLength(1000)]
    public string? ClientSecret { get; set; }

    [MaxLength(500)]
    public string? AuthorizationUrl { get; set; }

    [MaxLength(500)]
    public string? TokenUrl { get; set; }

    [MaxLength(500)]
    public string? UserInfoUrl { get; set; }

    [MaxLength(500)]
    public string? Scopes { get; set; }

    [MaxLength(100)]
    public string? TenantId { get; set; }

    public DateTime? LastSyncTime { get; set; }

    public int? LastSyncCount { get; set; }

    [MaxLength(500)]
    public string? LastSyncError { get; set; }

    [Column(TypeName = "jsonb")]
    public Dictionary<string, string>? AttributeMapping { get; set; }
}
