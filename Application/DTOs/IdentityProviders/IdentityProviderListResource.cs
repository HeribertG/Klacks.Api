using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Presentation.DTOs.IdentityProviders;

public class IdentityProviderListResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IdentityProviderType Type { get; set; }

    public bool IsEnabled { get; set; }

    public int SortOrder { get; set; }

    public bool UseForAuthentication { get; set; }

    public bool UseForClientImport { get; set; }

    public DateTime? LastSyncTime { get; set; }

    public int? LastSyncCount { get; set; }

    public string? LastSyncError { get; set; }
}
