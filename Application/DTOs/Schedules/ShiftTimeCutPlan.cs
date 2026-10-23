// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// What a cut_shift call would do, or why it cannot run: the plannable shift that is cut, its order, the cut operations
/// for the cut facade, the parts and the groups every part keeps.
/// </summary>
/// <param name="Refusal">Why the cut cannot run; null for a runnable plan</param>
/// <param name="OriginalShift">The plannable shift that is cut</param>
/// <param name="SealedOrderId">The order the parts belong to</param>
/// <param name="Operations">The batch for the cut facade (first part updates the plannable shift, the others are created)</param>
/// <param name="Parts">The parts in request order</param>
/// <param name="Groups">The groups of the plannable shift, kept by every part</param>
public sealed record ShiftTimeCutPlan(
    string? Refusal,
    Shift? OriginalShift,
    Guid SealedOrderId,
    IReadOnlyList<CutOperation> Operations,
    IReadOnlyList<ShiftTimeCutPart> Parts,
    IReadOnlyList<SimpleGroupResource> Groups)
{
    public bool IsRefused => Refusal != null;

    public static ShiftTimeCutPlan Refuse(string refusal) => new(refusal, null, Guid.Empty, [], [], []);
}
