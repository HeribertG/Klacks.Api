// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// What a cut_shift call would do, or why it cannot run: the plannable shift that is cut, its order, the cut operations
/// for the cut facade, the parts, the groups every part keeps and the default expenses every part carries.
/// </summary>
/// <param name="Refusal">Why the cut cannot run; null for a runnable plan</param>
/// <param name="OriginalShift">The plannable shift that is cut</param>
/// <param name="SealedOrderId">The order the parts belong to</param>
/// <param name="Operations">The batch for the cut facade (first part updates the plannable shift, the others are created)</param>
/// <param name="Parts">The parts in request order</param>
/// <param name="Groups">The groups of the plannable shift, kept by every part</param>
/// <param name="DefaultExpenses">The default expenses of the plannable shift: the first part keeps these rows, every other
/// part gets its own copy (ShiftCutFacade), so each work on any part is booked with them</param>
public sealed record ShiftTimeCutPlan(
    string? Refusal,
    Shift? OriginalShift,
    Guid SealedOrderId,
    IReadOnlyList<CutOperation> Operations,
    IReadOnlyList<ShiftTimeCutPart> Parts,
    IReadOnlyList<SimpleGroupResource> Groups,
    IReadOnlyList<ShiftExpenses> DefaultExpenses)
{
    public bool IsRefused => Refusal != null;

    public static ShiftTimeCutPlan Refuse(string refusal) => new(refusal, null, Guid.Empty, [], [], [], []);
}
