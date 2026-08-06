// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Scheduling;

/// <summary>
/// A permission to work on statutory holidays, as the admin sees and edits it.
/// </summary>
public class HolidayWorkExemptionResource
{
    public Guid Id { get; set; }

    /// <summary>
    /// Why the exemption exists, e.g. the statute permitting holiday work in this operation.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Null exempts everyone; otherwise only clients whose active contract references this scheduling
    /// rule are exempt.
    /// </summary>
    public Guid? SchedulingRuleId { get; set; }

    /// <summary>
    /// Set when the row came from a region package. Read-only: import identity, never editable.
    /// </summary>
    public string ImportSourceKey { get; set; } = string.Empty;
}
