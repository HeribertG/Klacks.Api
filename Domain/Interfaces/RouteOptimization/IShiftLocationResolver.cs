// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IShiftLocationResolver
{
    /// <summary>
    /// The coordinates of the first address of each shift's customer, from the stored coordinates first and freshly geocoded
    /// ones otherwise (the same lookup the container autofill uses for its candidates). A shift whose customer has no address
    /// with a city or whose address cannot be geocoded is absent from the result.
    /// </summary>
    /// <param name="shifts">Task shifts with their client and the client's addresses loaded</param>
    /// <param name="cancellationToken">Cancels the lookup</param>
    Task<IReadOnlyList<ShiftLocation>> LocateAsync(IReadOnlyList<Shift> shifts, CancellationToken cancellationToken);
}
