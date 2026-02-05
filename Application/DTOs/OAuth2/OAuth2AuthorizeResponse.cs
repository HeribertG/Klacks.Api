namespace Klacks.Api.Application.DTOs.OAuth2;

public class OAuth2AuthorizeResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
