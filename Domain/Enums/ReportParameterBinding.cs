// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Optional link between a report parameter and one of the query arguments the data
/// providers accept. Parameters without a binding are only available to filters and formulas.
/// </summary>
public enum ReportParameterBinding
{
    None = 0,
    GroupId = 1,
    ClientId = 2,
    StartDate = 3,
    EndDate = 4
}
