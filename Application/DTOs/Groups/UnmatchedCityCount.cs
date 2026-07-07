// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// An address city of ungrouped employees for which no equally named group exists, together with
/// the number of employees living there. Surfaced in the preview so the user can decide whether to
/// create the missing group instead of silently leaving these employees unassigned.
/// </summary>
/// <param name="City">The address city without a matching group.</param>
/// <param name="EmployeeCount">Number of ungrouped employees whose address city equals this value.</param>
public record UnmatchedCityCount(string City, int EmployeeCount);
