// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Export entry for expenses or reimbursements linked to a work entry.
/// @param Amount - The monetary amount
/// @param Taxable - True for taxable expenses, false for reimbursements
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class ExpensesExportEntry
{
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxable { get; set; }
}
