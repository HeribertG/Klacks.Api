// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IAbsenceCoverMaterializer
{
    /// <summary>
    /// Writes each reassignment into the scenario, as a move of the whole work or as a replacement.
    /// </summary>
    /// <param name="materialisation">The accepted repair of the run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<CoverMaterialisationResult> MaterialiseAsync(CoverMaterialisation materialisation, CancellationToken cancellationToken);

    /// <summary>
    /// Hands a moved work to another person, in the order of a cover move. The origin of the work stays. A work something
    /// holds in place (CoverMoveDecision.IsUntouched) stays where it is. Nothing stays staged unless the result is Done.
    /// </summary>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="workId">The scenario work</param>
    /// <param name="toClientId">Its new owner</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<MovedWorkHandOverResult> PassOnMovedWorkAsync(
        Guid analyseToken, Guid workId, Guid toClientId, CancellationToken cancellationToken);

    /// <summary>
    /// Gives a moved work back to the person it came from and clears its origin. A work something holds in place stays, and
    /// so does a work whose person is no longer free at that time. Nothing stays staged unless the result is Done.
    /// </summary>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="workId">The scenario work</param>
    /// <param name="ownerClientId">The person it came from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<MovedWorkHandOverResult> ReturnMovedWorkAsync(
        Guid analyseToken, Guid workId, Guid ownerClientId, CancellationToken cancellationToken);

    /// <summary>
    /// The clone source works this report may move as a whole, and the ones an earlier cover moved.
    /// </summary>
    /// <param name="workIdMap">Clone source work id to scenario clone id</param>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="report">How the absence reached the run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<CoverSourceWorks> FindSourceWorksAsync(
        IReadOnlyDictionary<Guid, Guid> workIdMap, Guid analyseToken, CoverReport report, CancellationToken cancellationToken);
}
