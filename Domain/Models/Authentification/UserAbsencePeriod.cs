// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Authentification;

/// <summary>
/// A closed date range during which a user is not reachable, e.g. holidays or sick leave.
/// Deliberately a separate table instead of a single from/until pair on AppUser: a user has
/// several absences over time, including future ones, and a single pair would be overwritten
/// by the next holiday. A range also expires by itself, unlike an on/off switch that rots.
/// </summary>
public class UserAbsencePeriod : BaseEntity
{
    public required string AppUserId { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}
