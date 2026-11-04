// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Owner rule of a service inside a container (Owner 2026-11-03): it belongs to the customer of its shift's order; a
/// service without customer belongs to the person of its container. The one definition for the container template
/// expansion, the container children save and the container split.
/// </summary>

namespace Klacks.Api.Domain.Services.Schedules;

public static class ContainerChildOwner
{
    /// <summary>The client a sub-work must carry.</summary>
    /// <param name="shiftClientId">Customer of the sub-work's shift, null or empty when the shift has none</param>
    /// <param name="containerClientId">Person of the container the sub-work lies in</param>
    public static Guid Of(Guid? shiftClientId, Guid containerClientId)
        => shiftClientId is { } customer && customer != Guid.Empty ? customer : containerClientId;
}
