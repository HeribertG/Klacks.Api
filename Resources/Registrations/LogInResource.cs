using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Resources.Registrations;

public class LogInResource
{
    [Required(ErrorMessage = "The e-mail address is required.")]
    [EmailAddress(ErrorMessage = "The e-mail address is invalid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "The password is required.")]
    public string Password { get; set; } = string.Empty;
}
