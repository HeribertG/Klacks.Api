// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IOvertimeCascadeService
{
    /// <summary>
    /// Reprocesses the K3/K4 overtime tier bands of the Works that follow <paramref name="work"/> in its
    /// basis period and persists them. MUST be called AFTER the surrounding UnitOfWork has committed the
    /// triggering change — both the successor lookup and each successor's prior-hours sum read committed
    /// database state, not the EF change tracker. No-op (and no extra save round-trip) when overtime is
    /// not configured or no successors exist.
    /// </summary>
    /// <param name="work">The Work that was just persisted (add/edit) or soft-deleted</param>
    /// <param name="previousState">For edits: the pre-edit snapshot; when its client, date, start time or scenario token differ from <paramref name="work"/>, the successors of the OLD position are reprocessed as well</param>
    Task ReprocessSuccessorsAsync(Work work, Work? previousState = null);

    /// <summary>
    /// Bulk variant: reprocesses the union of all successors of <paramref name="works"/>, each affected
    /// Work at most once, then persists in a single save. Same post-commit contract as the single overload.
    /// </summary>
    /// <param name="works">The Works that were just persisted or soft-deleted together</param>
    Task ReprocessSuccessorsAsync(IReadOnlyCollection<Work> works);
}
