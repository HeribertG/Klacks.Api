namespace Klacks.Api.Presentation.DTOs.Registrations;

public class RefreshRequestResource
{
    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;
}
