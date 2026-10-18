// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Where a booking refusal applies. The same cell is refused differently per entry path, so a diagnosis names each.
/// </summary>
/// <param name="DirectEntry">A single direct entry (POST Works, the schedule grid's save) is refused by the server</param>
/// <param name="Assistant">The assistant's placement of a single work refuses it</param>
/// <param name="Grid">The schedule grid itself does not offer the drop or menu entry, without asking the server</param>
public sealed record WorkBookingChannels(bool DirectEntry, bool Assistant, bool Grid)
{
    public bool RefusesAnyWrite => DirectEntry || Assistant;
}
