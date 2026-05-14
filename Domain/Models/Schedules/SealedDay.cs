// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Day-level seal record. A row with GroupId = null blocks all groups for that date,
/// rows with a concrete GroupId block only that group's members for that date.
/// </summary>
public class SealedDay : BaseEntity
{
    public DateOnly Date { get; set; }

    public Guid? GroupId { get; set; }

    public WorkLockLevel Level { get; set; } = WorkLockLevel.Closed;

    [MaxLength(2000)]
    public string? Reason { get; set; }

    public DateTime SealedAt { get; set; }

    [MaxLength(256)]
    public string SealedBy { get; set; } = string.Empty;
}
