// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Scheduling;

/// <summary>
/// Date window spanned by existing works that a thorough recalculation should cover.
/// </summary>
/// <param name="From">Earliest matching work date</param>
/// <param name="Until">Latest matching work date</param>
public sealed record WorkRecalculationWindow(DateOnly From, DateOnly Until);
