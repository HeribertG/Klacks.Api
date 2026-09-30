// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeIndexCoverageProbe
{
    /// <summary>
    /// Share (0..1) of the skills and recipes the current catalogue requires that already have a stored
    /// index row, regardless of whether the row is up to date. 1 when the catalogue is empty.
    /// </summary>
    Task<double> GetStoredCoverageAsync(CancellationToken cancellationToken);
}
