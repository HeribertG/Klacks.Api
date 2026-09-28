// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Read-only result of the grouping planner.
/// </summary>
/// <param name="AnchorGroupCount">Number of distinct groups that can receive clients in this run: every group carrying coordinates plus every group whose name was actually matched against an address city. Zero means neither path can place anyone.</param>
/// <param name="Assignments">The planned moves, one per client that would change its location membership.</param>
/// <param name="Unassigned">Clients that cannot be placed, each with the reason why.</param>
public record CustomerGroupingProposal(
    int AnchorGroupCount,
    IReadOnlyList<CustomerGroupingAssignment> Assignments,
    IReadOnlyList<UnassignedCustomer> Unassigned);
