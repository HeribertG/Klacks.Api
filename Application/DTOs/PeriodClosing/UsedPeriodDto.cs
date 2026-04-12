// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// Represents a billing period that has actually been used: a distinct
/// (StartDate, EndDate, PaymentInterval, GroupId) tuple for which at least one
/// non-deleted work or break entry exists for a non-deleted client.
/// </summary>
/// <param name="GroupId">Optional group the client belongs to via GroupItem</param>
/// <param name="GroupName">Display name of the group, null when no group</param>
public class UsedPeriodDto
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public PaymentInterval PaymentInterval { get; set; }

    public Guid? GroupId { get; set; }

    public string? GroupName { get; set; }
}
