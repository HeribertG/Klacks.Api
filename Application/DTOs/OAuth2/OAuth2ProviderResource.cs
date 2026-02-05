using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.OAuth2;

public class OAuth2ProviderResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IdentityProviderType Type { get; set; }
}
