// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO for default expenses attached to a shift template.
/// </summary>
/// <param name="ShiftId">The shift this default expense belongs to</param>
/// <param name="Amount">Expense amount in currency</param>
/// <param name="Description">Short description of the expense</param>
/// <param name="Taxable">True = taxable (Spesen), False = reimbursement (Vergütung)</param>
namespace Klacks.Api.Application.DTOs.Schedules;

public class ShiftExpensesResource
{
    public Guid Id { get; set; }

    public Guid ShiftId { get; set; }

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Taxable { get; set; }
}
