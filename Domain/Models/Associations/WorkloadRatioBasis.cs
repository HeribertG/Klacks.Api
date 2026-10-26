// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Associations;

/// <summary>
/// The two figures a ratio-based workload percent was computed from. Present only when the percent is a
/// ratio (settings, contract or scheduling rule); null when it comes from a Percent field or a fallback.
/// </summary>
/// <param name="GuaranteedHours">Guaranteed hours used as the numerator</param>
/// <param name="FullTime">Full-time hours used as the denominator</param>
public sealed record WorkloadRatioBasis(decimal GuaranteedHours, decimal FullTime);
