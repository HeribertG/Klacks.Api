namespace Klacks.Api.Application.DTOs.OAuth2;

public class OAuth2CallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
