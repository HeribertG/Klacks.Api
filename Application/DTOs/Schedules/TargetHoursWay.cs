// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The four ways an employee's target hours are determined, named as the setup guide names them.
/// </summary>
public enum TargetHoursWay
{
    /// <summary>No contract: monthly row, otherwise the settings, always at 100 %.</summary>
    NoContract = 0,

    /// <summary>Contract with fixed guaranteed hours (or a scheduling rule setting them).</summary>
    FixedHours = 1,

    /// <summary>Weg 1: payment interval "Monthly (target hours table)" plus percent; monthly row x percent, contract hours where no row exists.</summary>
    MonthlyTableInterval = 2,

    /// <summary>Weg 2: guaranteed hours left empty plus percent; (monthly row ?? settings) x percent on any interval.</summary>
    InheritedCompanyValue = 3
}
