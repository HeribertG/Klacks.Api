// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The cut of one plannable shift into one part per weekday, as the operations ShiftCutFacade processes.
/// </summary>
/// <param name="Source">The plannable shift that is cut</param>
/// <param name="SealedOrderId">The sealed order the parts descend from</param>
/// <param name="Parts">The parts, in weekday order; the first reuses the plannable shift</param>
/// <param name="Operations">The cut operations, one per part</param>

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ShiftWeekdayCutPlan(
    Shift Source,
    Guid SealedOrderId,
    IReadOnlyList<ShiftWeekdayCutPart> Parts,
    IReadOnlyList<CutOperation> Operations);
