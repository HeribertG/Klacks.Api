// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Registrations;

public class LogInResource
{
    [Required(ErrorMessage = "Email or username is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "The password is required.")]
    public string Password { get; set; } = string.Empty;
}
