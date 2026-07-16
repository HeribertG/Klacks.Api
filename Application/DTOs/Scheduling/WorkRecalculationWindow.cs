// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Scheduling;

/// <summary>
/// Date window spanned by existing works that a thorough recalculation should cover.
/// </summary>
/// <param name="From">Earliest matching work date</param>
/// <param name="Until">Latest matching work date</param>
public sealed record WorkRecalculationWindow(DateOnly From, DateOnly Until);
