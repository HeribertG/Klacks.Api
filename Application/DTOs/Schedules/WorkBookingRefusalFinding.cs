// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One reason why a booking is refused now, with the channels it applies to and the remedy.
/// </summary>
/// <param name="Reason">Which rule refuses the booking</param>
/// <param name="Code">Refusal code the server answers on the direct entry path (WorkBookingRefusalCodes or WorkWriteConflictCodes); null when no server path carries a code for it</param>
/// <param name="Channels">Entry paths the refusal applies to</param>
/// <param name="Remedy">What lifts the refusal</param>
/// <param name="Details">Fields named after the refusal code field names (dates, bounds, counts, seal kind, occupying shift)</param>
/// <param name="Conflicts">Schedule conflicts behind a conflict refusal; empty for other reasons</param>
public sealed record WorkBookingRefusalFinding(
    WorkBookingRefusalReason Reason,
    string? Code,
    WorkBookingChannels Channels,
    WorkBookingRemedy Remedy,
    IReadOnlyDictionary<string, object?> Details,
    IReadOnlyList<WorkConflictItem> Conflicts);
