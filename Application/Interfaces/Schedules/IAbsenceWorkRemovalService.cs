// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IAbsenceWorkRemovalService
{
    /// <summary>
    /// The works these absences take away: an on-call absence takes none, and a sealed, replaced or foreign-group work stays.
    /// </summary>
    /// <param name="absences">The absences</param>
    /// <param name="analyseToken">Scenario of the absences; null is the real plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<RemovableWork>> FindAsync(
        IReadOnlyList<AbsenceSpan> absences, Guid? analyseToken, CancellationToken cancellationToken);

    /// <summary>
    /// Writes the absences and deletes the works they take away as one transaction, with everything on the works. A failure
    /// leaves nothing staged. Without works to delete it only writes, so it can run inside an open transaction. Call it
    /// after the absence credit is calculated.
    /// </summary>
    /// <param name="taken">The works <see cref="FindAsync"/> named</param>
    /// <param name="writeAbsences">Stages the absences; it can run more than once</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WriteAndRemoveAsync(
        IReadOnlyList<RemovableWork> taken, Func<Task> writeAbsences, CancellationToken cancellationToken);
}
