// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Id and from date of a shift, the two columns a scan needs to rank or fingerprint its candidates without loading
/// full rows.
/// </summary>
/// <param name="Id">Shift id</param>
/// <param name="FromDate">First day of the shift</param>
public sealed record ShiftDateKey(Guid Id, DateOnly FromDate);
