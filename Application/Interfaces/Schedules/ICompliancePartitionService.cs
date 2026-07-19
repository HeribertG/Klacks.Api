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
}
