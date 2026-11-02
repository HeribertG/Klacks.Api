// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IAbsenceCoverMaterializer
{
    /// <summary>
    /// Writes each reassignment into the scenario, as a move of the whole work or as a replacement.
    /// </summary>
    /// <param name="materialisation">The accepted repair of the run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scenario works that moved</returns>
    Task<IReadOnlySet<Guid>> MaterialiseAsync(CoverMaterialisation materialisation, CancellationToken cancellationToken);
}
