// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Registrations;

public class ChangePasswordResource
{
    public string AppName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    [Required]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
}
