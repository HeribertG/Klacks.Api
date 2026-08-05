// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Shared accept/block partition over a batch of planned work rows: one batched pre-commit check,
/// the K1 supervisor-override path for Block-mode escalations, and a greedy per-row fallback for
/// clients whose batch result contains an Error. Extracted so propose_plan, cover_absence and any
/// future batch writer share one partition semantics instead of duplicating it per handler.
/// </summary>
public interface ICompliancePartitionService
{
    Task<CompliancePartitionResult> PartitionAsync(
        IReadOnlyList<PlannedWorkRow> rows,
        Guid? analyseToken,
        bool overrideBlockRequested,
        CancellationToken cancellationToken);

    /// <summary>
    /// Partitions atomic repair options. Unlike PartitionAsync this never splits an option, so a swap
    /// chain is applied whole or not at all.
    /// </summary>
    /// <param name="options">Repair options in submission order.</param>
    /// <param name="analyseToken">Scenario token the plan lives in, null for the real plan.</param>
    /// <param name="overrideBlockRequested">True when the caller asked for a supervisor override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OptionPartitionResult> PartitionOptionsAsync(
        IReadOnlyList<PlannedOption> options,
        Guid? analyseToken,
        bool overrideBlockRequested,
        CancellationToken cancellationToken);
}
