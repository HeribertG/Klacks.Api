// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// DTO for default expenses attached to a shift template.
/// </summary>
/// <param name="ShiftId">The shift this default expense belongs to</param>
/// <param name="Amount">Expense amount in currency</param>
/// <param name="Description">Short description of the expense</param>
/// <param name="Taxable">True = taxable wage supplement (Vergütung), False = non-taxable reimbursement of advanced money (Spesen)</param>
namespace Klacks.Api.Application.DTOs.Schedules;

public class ShiftExpensesResource
{
    public Guid Id { get; set; }

    public Guid ShiftId { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxable { get; set; }
}
