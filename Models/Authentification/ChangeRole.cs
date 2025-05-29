namespace Klacks.Api.Models.Authentification;

public class ChangeRole
{
    public bool IsSelected { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}
