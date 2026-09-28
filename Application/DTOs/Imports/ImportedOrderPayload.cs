// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Imports;

public class ImportedOrderPayload
{
    public string SourceSystemId { get; set; } = string.Empty;

    public string ExternalOrderReference { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? DurationMinutes { get; set; }

    public ImportedCustomerPayload Customer { get; set; } = new();

    public DateOnly FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsTimeRange { get; set; }

    public bool IsMonday { get; set; }

    public bool IsTuesday { get; set; }

    public bool IsWednesday { get; set; }

    public bool IsThursday { get; set; }

    public bool IsFriday { get; set; }

    public bool IsSaturday { get; set; }

    public bool IsSunday { get; set; }

    public int Quantity { get; set; } = 1;

    public int SumEmployees { get; set; } = 1;
}
