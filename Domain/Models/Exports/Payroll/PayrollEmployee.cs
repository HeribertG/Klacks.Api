// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollEmployee
{
    public Guid ClientId { get; set; }

    public int IdNumber { get; set; }

    public string FullName { get; set; } = string.Empty;

    public List<PayrollDayEntry> Entries { get; set; } = [];
}
