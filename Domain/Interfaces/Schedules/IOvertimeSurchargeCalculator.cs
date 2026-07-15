// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IOvertimeSurchargeCalculator
{
    /// <summary>
    /// Computes the K3 overtime surcharge portions for a single Work by cumulating the client's other
    /// worked hours in the configured basis period (day or week) and splitting this Work's own hours
    /// into the configured tier bands. Returns OvertimeCalculationResult.None when overtime is not
    /// configured for the installation.
    /// </summary>
    /// <param name="work">The Work whose hours are being placed into overtime tier bands</param>
    Task<OvertimeCalculationResult> CalculateAsync(Work work);

    /// <summary>
    /// Returns the other Works of the same client, AnalyseToken and basis period (day or week) that sort
    /// after <paramref name="work"/> in the deterministic (CurrentDate, StartTime, Id) order — i.e. the
    /// Works whose own "prior hours" sum includes <paramref name="work"/> and therefore need their tier
    /// bands recomputed whenever <paramref name="work"/> is added, edited or deleted. Returns an empty
    /// list when overtime is not configured for the installation (no tiers).
    /// </summary>
    /// <param name="work">The Work that was just saved or deleted</param>
    Task<IReadOnlyList<Work>> GetSuccessorWorksAsync(Work work);
}
