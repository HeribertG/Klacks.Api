// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IAdminAutonomyLevelAggregator. Walks the admin ids in ordinal order and keeps the lowest
/// level together with the admin who holds it; the ordering is not cosmetic, without it the tie-break
/// would follow hash order and the audit would name a different admin from one run to the next for the
/// same configuration. An admin without a stored row is either read as AutonomyDefaults.DefaultLevel or
/// blocks the whole aggregation, depending on the policy the caller passes.
/// </summary>
/// <param name="audienceResolver">Lists the admin users whose autonomy levels are aggregated.</param>
/// <param name="autonomyPreferences">Per-admin autonomy level rows.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public sealed class AdminAutonomyLevelAggregator : IAdminAutonomyLevelAggregator
{
    private static readonly AdminAutonomyAggregate NoAdmins = new(null, null, null);

    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly IAgentAutonomyPreferenceRepository _autonomyPreferences;

    public AdminAutonomyLevelAggregator(
        IPlanningAudienceResolver audienceResolver,
        IAgentAutonomyPreferenceRepository autonomyPreferences)
    {
        _audienceResolver = audienceResolver;
        _autonomyPreferences = autonomyPreferences;
    }

    public async Task<AdminAutonomyAggregate> AggregateAsync(
        AdminAutonomyMissingPreferencePolicy missingPreferencePolicy,
        CancellationToken cancellationToken = default)
    {
        var adminIds = await _audienceResolver.GetAdminUserIdsAsync(cancellationToken);
        if (adminIds.Count == 0)
        {
            return NoAdmins;
        }

        var minimum = AutonomyLevel.FullyAutonomous;
        Guid? decidingAdminUserId = null;
        var firstAdminSeen = false;

        foreach (var adminId in adminIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            var row = await _autonomyPreferences.GetAsync(adminId, cancellationToken);
            if (row is null && missingPreferencePolicy == AdminAutonomyMissingPreferencePolicy.Block)
            {
                return new AdminAutonomyAggregate(null, null, adminId);
            }

            var level = row?.Level ?? AutonomyDefaults.DefaultLevel;
            if (firstAdminSeen && level >= minimum)
            {
                continue;
            }

            firstAdminSeen = true;
            minimum = level;
            decidingAdminUserId = Guid.TryParse(adminId, out var parsed) ? parsed : null;
        }

        return new AdminAutonomyAggregate(minimum, decidingAdminUserId, null);
    }
}
