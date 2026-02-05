namespace Klacks.Api.Application.DTOs.Registrations;

public class TokenResource
{
    public bool Success { get; set; }

    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime ExpTime { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsAuthorised { get; set; }

    public string Version { get; set; } = string.Empty;
}
