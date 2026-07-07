// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Exports;

public class ClientPeriodHoursExportEntry
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal Hours { get; set; }

    public decimal Surcharges { get; set; }

    public string PaymentInterval { get; set; } = string.Empty;
}
