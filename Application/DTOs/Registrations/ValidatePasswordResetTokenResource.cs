// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Registrations;

public class ValidatePasswordResetTokenResource
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}
