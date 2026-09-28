// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingUnitView(
    string Unit,
    int DutiesAnalysed,
    int DutiesNobodyInUnitCanTake,
    int DutiesNobodyInCompanyCanTake,
    int EmployeesInScope,
    int EmployeesWithoutActiveContract,
    string? DominantBlockingReason,
    int DominantBlockingReasonEmployees,
    IReadOnlyList<GroupingCapacityGapView> CapacityShortfalls);
