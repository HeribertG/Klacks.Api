// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The two rules that keep "no customer" an intentional state rather than a state something fell into.
///
/// A duty without a customer is a real category in Klacks, not a defect: refuelling, vehicle care and
/// cleaning are paid working time that no customer is billed for, and whole professions — nursing on a
/// ward, a cook in the kitchen, a hairdresser in the salon, back office — occupy a POSITION rather than
/// fulfil an order. In such a business every single duty is clientless. Working time counts either way,
/// because a Work row's ClientId is the EMPLOYEE and the customer hangs off the Shift.
///
/// What must never happen is that a duty ENDS UP without a customer by accident, because then a
/// clientless duty and a half-finished order become indistinguishable — both are just ClientId = null —
/// and the one question the category exists for ("what of this is not billable?") can no longer be
/// answered. The frontend's isClientless flag cannot carry that meaning: it lives in the browser and is
/// gone after a reload (see Klacks.Ui commit 5aab341f, "frontend-only and transient").
///
/// So the STATUS carries it instead, which needs no new column: an order still being drafted always
/// names its customer, and a duty that deliberately has none is sealed the moment it is created. That
/// is the owner's decision of 2026-09-07 and it makes both rules below decidable from what is stored:
///
/// 1. OriginalOrder without a customer is refused. A draft is by definition a customer's request that
///    is still being worked out; without the customer it is unfinished, not intentional.
/// 2. A clientless order is created SEALED, and sealing is irreversible — so it must satisfy the same
///    requirements SealAsync checks before it is written at all. AddWithSealedOrderHandling clones
///    without checking (ShiftRepository.cs:472), so without this an incomplete duty would become
///    permanent, and "delete and start over" would be the only way out.
///
/// Deliberately narrow: rule 2 applies only to orders WITHOUT a customer. Every existing path that
/// creates a sealed order with one — the ERP import, create_shift, the seeds — keeps behaving exactly
/// as before, because tightening those is a separate decision with its own blast radius.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ClientlessOrderRules
{
    public const string DraftWithoutCustomerMessage =
        "An order still in draft must name the customer it is billed to. A duty that deliberately has no "
        + "customer (internal work such as refuelling, cleaning, or a position like a ward or kitchen "
        + "shift) is created as a sealed order instead, never as a draft.";

    public static string IncompleteClientlessOrderMessage(IReadOnlyList<string> missing) =>
        "A duty without a customer is sealed the moment it is created, and sealing cannot be undone, so "
        + "it has to be complete first. Missing: " + string.Join(", ", missing) + ".";

    /// <summary>
    /// True when the shift is a draft order carrying no customer — the state rule 1 refuses.
    /// Containers are exempt: they are templates rather than orders and never name a customer.
    /// </summary>
    public static bool IsDraftWithoutCustomer(Shift shift) =>
        shift.ShiftType != ShiftType.IsContainer
        && shift.Status == ShiftStatus.OriginalOrder
        && shift.ClientId == null;

    /// <summary>
    /// True when the shift is a sealed order carrying no customer — the case rule 2 requires to be
    /// complete before it is written.
    /// </summary>
    public static bool IsSealedWithoutCustomer(Shift shift) =>
        shift.ShiftType != ShiftType.IsContainer
        && shift.Status == ShiftStatus.SealedOrder
        && shift.ClientId == null;
}
