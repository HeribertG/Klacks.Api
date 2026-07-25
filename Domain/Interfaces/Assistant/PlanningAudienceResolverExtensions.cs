// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic single-admin resolution for background flows that need to act under some
/// operator identity but have no logged-in user (e.g. an unattended LLM call). Picks the
/// lowest-sorted admin id so repeated calls within the same admin set agree on the same user.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public static class PlanningAudienceResolverExtensions
{
    public static async Task<string> GetFirstAdminUserIdAsync(
        this IPlanningAudienceResolver audienceResolver, CancellationToken cancellationToken = default)
    {
        var adminIds = (await audienceResolver.GetAdminUserIdsAsync(cancellationToken))
            .Where(id => Guid.TryParse(id, out _))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return adminIds.Count > 0 ? adminIds[0] : string.Empty;
    }
}
