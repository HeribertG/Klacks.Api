// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// The validity of a shift (its first and last day), read as a projection when a caller needs only the period, not the
/// shift itself.
/// </summary>
/// <param name="FromDate">First day the shift is valid</param>
/// <param name="UntilDate">Last day the shift is valid; null = open end</param>
public sealed record ShiftValidityPeriod(DateOnly FromDate, DateOnly? UntilDate);
