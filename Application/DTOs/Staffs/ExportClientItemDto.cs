// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Staffs;

/// <summary>
/// A single client row returned for CSV export.
/// </summary>
public class ExportClientItemDto
{
    public int IdNumber { get; set; }
    public string Company { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? Birthdate { get; set; }
    public int Gender { get; set; }
    public int Type { get; set; }
    public bool LegalEntity { get; set; }
}
