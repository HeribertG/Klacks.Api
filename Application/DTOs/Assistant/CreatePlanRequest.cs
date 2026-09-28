// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Assistant;

public class CreatePlanRequest
{
    [Required]
    public string Goal { get; set; } = string.Empty;

    public string? SessionId { get; set; }
}
