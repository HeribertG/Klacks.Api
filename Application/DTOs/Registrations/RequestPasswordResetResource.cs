using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Presentation.DTOs.Registrations;

public class RequestPasswordResetResource
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;
}