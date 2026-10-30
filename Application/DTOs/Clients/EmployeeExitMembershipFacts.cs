// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Clients;

/// <summary>
/// The active memberships of the employee, as the recording skill's lone-match rule sees them (not ended before the
/// company's today).
/// </summary>
/// <param name="ActiveCount">Number of active memberships</param>
/// <param name="ValidFrom">Start of the lone active membership; null unless exactly one is active</param>
/// <param name="ValidUntil">End of the lone active membership; null when it is open-ended or not exactly one is active</param>
/// <param name="Options">Id, start and end of every active membership when more than one is active; empty otherwise</param>
public sealed record EmployeeExitMembershipFacts(
    int ActiveCount,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    string Options);
