// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Assistant;

public class CreatePlanRequest
{
    [Required]
    public string Goal { get; set; } = string.Empty;

    public string? SessionId { get; set; }
}
