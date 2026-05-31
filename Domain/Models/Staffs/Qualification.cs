// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A named qualification / skill an employee can hold and a shift can require
/// (e.g. "Forklift licence", "First aid"). Soft-deletable master entity.
/// </summary>

using Klacks.Api.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Staffs;

public class Qualification : BaseEntity
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
