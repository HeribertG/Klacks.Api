// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
