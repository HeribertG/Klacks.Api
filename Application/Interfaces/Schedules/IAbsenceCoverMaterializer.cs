// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
    /// The clone source works this report may move, the ones a cover could move as a whole.
    /// </summary>
    /// <param name="workIdMap">Clone source work id to scenario clone id</param>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="report">How the absence reached the run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlySet<Guid>> FindMovableSourceWorksAsync(
        IReadOnlyDictionary<Guid, Guid> workIdMap, Guid analyseToken, CoverReport report, CancellationToken cancellationToken);
}
