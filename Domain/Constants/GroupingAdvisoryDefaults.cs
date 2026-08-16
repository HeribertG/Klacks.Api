// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared defaults for the grouping-advisory skills (evaluate_location_group_candidates and, later,
/// evaluate_grouping_by_qualification). MinViableGroupSize is a business judgment call, not a technical
/// one — kept as one named constant for both criteria until a real case shows they need to differ.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GroupingAdvisoryDefaults
{
    public const int MinViableGroupSize = 3;
}
