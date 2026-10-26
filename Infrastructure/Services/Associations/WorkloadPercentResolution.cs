// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Infrastructure.Services.Associations;

/// <summary>
/// Outcome of resolving the workload share for one client and day: the percent, where it came from and,
/// for a ratio, the two figures it was computed from.
/// </summary>
/// <param name="Percent">Workload share in percent</param>
/// <param name="Source">Master data that decided the share</param>
/// <param name="RatioBasis">Numerator and denominator of a ratio-based share; null otherwise</param>
internal sealed record WorkloadPercentResolution(decimal Percent, WorkloadPercentSource Source, WorkloadRatioBasis? RatioBasis);
