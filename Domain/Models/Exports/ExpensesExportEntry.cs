// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Export entry for an expense (non-taxable reimbursement) or an allowance (taxable wage supplement) linked to a work entry.
/// @param Amount - The monetary amount
/// @param Taxable - True for a taxable allowance (Vergütung), false for a non-taxable expense reimbursement (Spesen)
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class ExpensesExportEntry
{
    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxable { get; set; }
}
