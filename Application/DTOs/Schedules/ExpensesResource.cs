// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class ExpensesResource
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public WorkResource? Work { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxable { get; set; }

    /// <summary>
    /// Three-day schedule snapshot the Post/Put/Delete handlers populate so the
    /// frontend can update the grid in place without a separate refresh call.
    /// </summary>
    public List<WorkScheduleResource> ScheduleEntries { get; set; } = [];
}
