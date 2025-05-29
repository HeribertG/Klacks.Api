namespace Klacks.Api.Resources.Registrations;

public class AuthenticationResource
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public bool IsAuthorised { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
}
