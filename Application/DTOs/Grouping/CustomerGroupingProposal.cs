// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

public record CustomerGroupingProposal(
    int AnchorGroupCount,
    IReadOnlyList<CustomerGroupingAssignment> Assignments,
    IReadOnlyList<UnassignedCustomer> Unassigned);
