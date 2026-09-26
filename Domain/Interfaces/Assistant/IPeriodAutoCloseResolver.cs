// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of the autonomy gates for the autonomous period close. The service asks it once when it
/// evaluates a group and again immediately before it seals, so a kill switch flipped or a level lowered in
/// between withdraws consent for that very seal.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPeriodAutoCloseResolver
{
    /// <summary>
    /// Resolves the gates for one group; the governance rule of that group wins over the installation-wide one.
    /// Never returns null.
    /// </summary>
    Task<PeriodAutoCloseDecision> ResolveAsync(Guid groupId, CancellationToken cancellationToken = default);
}
