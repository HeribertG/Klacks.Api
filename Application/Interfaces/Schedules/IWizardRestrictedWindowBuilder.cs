// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the active RestrictedTimeWindowRule set (K16) into the GA-consumable
/// <see cref="Klacks.ScheduleOptimizer.Models.CoreRestrictedTimeWindow"/> list for one wizard run: each
/// rule's group tag is expanded into the concrete set of restricted shift ids among the period's shifts.
/// </summary>

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IWizardRestrictedWindowBuilder
{
    Task<IReadOnlyList<CoreRestrictedTimeWindow>> BuildAsync(
        IReadOnlyList<Guid> shiftIds,
        DateOnly from,
        DateOnly until,
        CancellationToken ct);
}
