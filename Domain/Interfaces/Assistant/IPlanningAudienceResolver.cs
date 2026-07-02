// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the set of user ids that belong to a planning role (Admin or Authorised). Used to gate
/// operational proactive alerts so that regular employees never receive them.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPlanningAudienceResolver
{
    Task<IReadOnlySet<string>> GetPlanningUserIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves users in the Admin role only, for alerts that are a data/integration concern
    /// (e.g. an ERP import failure) rather than a scheduling gap every planner should see.
    /// </summary>
    Task<IReadOnlySet<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default);
}
