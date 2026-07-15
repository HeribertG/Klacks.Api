// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// The period over which a client's worked hours are cumulated before comparing them against the
/// configured overtime tier thresholds (K3). Day resets the cumulation every calendar day, Week
/// cumulates across the configured week (see IWeekConfiguration).
/// </summary>
public enum OvertimeBasis
{
    Day = 0,
    Week = 1
}
