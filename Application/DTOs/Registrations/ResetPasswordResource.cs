// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Registrations;

public class ResetPasswordResource
{
    [Required(ErrorMessage = "Reset token is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).*$",
        ErrorMessage = "Password must contain at least one digit and one special character.")]
    public string Password { get; set; } = string.Empty;
}
