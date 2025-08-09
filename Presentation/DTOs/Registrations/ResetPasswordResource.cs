namespace Klacks.Api.Presentation.DTOs.Registrations;

public class ResetPasswordResource
{
    public string Token { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
