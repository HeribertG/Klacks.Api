// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Day-level seal record. A row with GroupId = null blocks all groups for that date,
/// rows with a concrete GroupId block only that group's members for that date (members by GroupItem/Membership,
/// or clients who worked a shift of the group that day). Level Closed rows come from a period close and are lifted by
/// that group's reopen; Level Approved rows come from a day approval and are lifted only by the approving group's
/// revoke. Every non-deleted row locks the day, even when the day holds no entries.
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
