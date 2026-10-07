// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollDayEntry
{
    public DateOnly Date { get; set; }

    public PayrollEntryKind Kind { get; set; }

    public decimal Quantity { get; set; }

    public PayrollQuantityUnit Unit { get; set; } = PayrollQuantityUnit.Hours;

    public Guid? AbsenceId { get; set; }
}
