// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of the list_expiring_contracts skill: contract assignments and (optionally)
/// qualifications expiring within a given horizon, combined and sorted by expiry date ascending.
/// </summary>
/// <param name="WithinDays">Horizon in days that was scanned, relative to the company's current date.</param>
/// <param name="IncludeQualifications">Whether qualifications were included in the scan.</param>
/// <param name="TotalContractCount">Total matching contract assignments within the horizon, before Limit was applied.</param>
/// <param name="TotalQualificationCount">Total matching qualifications within the horizon, before Limit was applied.</param>
/// <param name="Items">Combined, expiry-date-ascending list of matches, capped at the requested limit.</param>

namespace Klacks.Api.Application.DTOs.Staffs;

public record ListExpiringContractsResult(
    int WithinDays,
    bool IncludeQualifications,
    int TotalContractCount,
    int TotalQualificationCount,
    IReadOnlyList<ExpiringItem> Items);
