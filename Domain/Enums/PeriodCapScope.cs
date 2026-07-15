// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What kind of hours a <see cref="Klacks.Api.Domain.Models.Scheduling.PeriodCapRule"/> caps. Stage 1
/// (K5) supports only TotalHours. An OvertimeHours scope needs the overtime-vs-regular hour split from
/// K3/K4 and is deliberately not implemented yet — do not add it until that split exists.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum PeriodCapScope
{
    TotalHours = 0,
}
