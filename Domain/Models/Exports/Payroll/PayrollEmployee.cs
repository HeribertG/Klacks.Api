// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollEmployee
{
    public Guid ClientId { get; set; }

    public int IdNumber { get; set; }

    public string FullName { get; set; } = string.Empty;

    public List<PayrollDayEntry> Entries { get; set; } = [];
}
