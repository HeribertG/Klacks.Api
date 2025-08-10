using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Domain.Models.Authentification;

public class AppUser : IdentityUser
{
    // Extended Properties
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

}
