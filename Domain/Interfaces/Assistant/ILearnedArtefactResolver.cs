// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the artefacts the loop has activated, in one shape regardless of whether they are phrases or
/// capabilities. Shared by the fitness pass and the pruner so the two can never disagree about what is
/// currently live.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILearnedArtefactResolver
{
    Task<IReadOnlyList<LearnedArtefact>> ListActiveAsync(int limit, CancellationToken cancellationToken = default);
}
