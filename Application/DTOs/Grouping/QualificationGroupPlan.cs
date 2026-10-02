// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Read-only result of QualificationGroupPlanner: the qualification groups to create or reuse with their
/// members, the qualifications below the minimum, and the counts the preview reports.
/// </summary>
/// <param name="TotalClients">Clients of the requested types that were loaded.</param>
/// <param name="ConsideredClients">Clients inside the scope (all clients without a scope).</param>
/// <param name="SkippedAlreadyGroupedCount">Considered clients skipped because they already hold a membership and includeAlreadyGrouped was false.</param>
/// <param name="ClientsWithoutQualificationCount">Considered clients holding no qualification valid today.</param>
/// <param name="Groups">Planned groups ordered by name.</param>
/// <param name="Skipped">Qualifications below the minimum, ordered by name.</param>
/// <param name="Warnings">Non-fatal issues, e.g. two qualifications with the same name merged into one group.</param>
public sealed record QualificationGroupPlan(
    int TotalClients,
    int ConsideredClients,
    int SkippedAlreadyGroupedCount,
    int ClientsWithoutQualificationCount,
    IReadOnlyList<PlannedQualificationGroup> Groups,
    IReadOnlyList<SkippedQualificationGroup> Skipped,
    IReadOnlyList<string> Warnings);
