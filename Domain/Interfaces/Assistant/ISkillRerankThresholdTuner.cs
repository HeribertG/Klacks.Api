// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Learns the margin threshold of the Adaptive skill rerank from real turns (owner decision 2026-10-24: learning by
/// doing, automatic, inside fixed limits). One threshold per installation, kept in the settings table, because
/// the catalogue and the embedder - what the margin measures - are shared by every user of the installation.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRerankThresholdTuner
{
    /// <summary>Loads the persisted threshold into the live one, so a restart does not forget what was learned.</summary>
    Task RestoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the next threshold from the recent turns, applies and persists it. Returns the new value, or null
    /// when the installation does not learn (mode other than Adaptive) or the turns are too few.
    /// </summary>
    Task<double?> TuneAsync(CancellationToken cancellationToken = default);
}
