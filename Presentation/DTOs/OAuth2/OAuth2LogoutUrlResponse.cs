namespace Klacks.Api.Presentation.DTOs.OAuth2;

public class OAuth2LogoutUrlResponse
{
    public string? LogoutUrl { get; set; }
    public bool SupportsLogout { get; set; }
}
