// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;

namespace Klacks.Api.Application.Interfaces.Schedules.AutoWizard;

/// <summary>
/// The server's record of which autofill runs are going right now, so every viewer of a group can see a run that
/// someone else started. In memory only: a restart forgets every run, exactly like it ends them.
/// </summary>
public interface IAutofillRunPresenceTracker
{
    /// <summary>
    /// Records a run and announces it. Never throws: a failed announcement must not stop the run.
    /// </summary>
    /// <param name="groupId">Group the run's scenarios belong to; null = no group</param>
    /// <param name="periodFrom">First day of the planned period</param>
    /// <param name="periodUntil">Last day of the planned period</param>
    Task<AutofillRunPresenceDto> BeginAsync(Guid? groupId, DateOnly periodFrom, DateOnly periodUntil);

    /// <summary>
    /// Forgets a run and announces its end. Never throws; an unknown run id is ignored.
    /// </summary>
    /// <param name="runId">Presence id returned by BeginAsync</param>
    Task EndAsync(Guid runId);

    /// <summary>
    /// Every recorded run, unfiltered; callers must apply the viewer's group visibility.
    /// </summary>
    IReadOnlyList<AutofillRunPresenceDto> GetActive();
}
