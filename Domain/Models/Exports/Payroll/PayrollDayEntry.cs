// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollDayEntry
{
    public DateOnly Date { get; set; }

    public PayrollEntryKind Kind { get; set; }

    public decimal Quantity { get; set; }

    public Guid? AbsenceId { get; set; }
}
