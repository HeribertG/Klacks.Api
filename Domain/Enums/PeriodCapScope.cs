// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What kind of hours a <see cref="Klacks.Api.Domain.Models.Scheduling.PeriodCapRule"/> caps.
/// TotalHours caps every persisted period hour (works, breaks, work changes). OvertimeHours caps only
/// the hours above the client's overtime threshold, derived per basis unit (day or week) from the SAME
/// overtime configuration chain the K3/K4 surcharge engine resolves (rule ladder, dated revision
/// snapshot, global settings, contract OvertimeThreshold fallback) — never a second overtime definition.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum PeriodCapScope
{
    TotalHours = 0,

    OvertimeHours = 1,
}
