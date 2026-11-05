// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Identifies one hourly availability slot of a client; at most one availability row exists per slot.
/// </summary>
/// <param name="ClientId">Client the slot belongs to</param>
/// <param name="Date">Calendar day of the slot</param>
/// <param name="Hour">Hour of the day (0-23)</param>

namespace Klacks.Api.Domain.Models.Staffs;

public record ClientAvailabilitySlot(Guid ClientId, DateOnly Date, int Hour);
