// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Retires recipe runs that were started but never closed, so the funnel denominator stays honest.
/// </summary>
public interface IRecipeRunExpirySweep
{
    /// <summary>Expires every stale Running row and returns how many were flipped.</summary>
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
