// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default INextPeriodAutonomyResolver. The minimum autonomy level over all admin users, additionally
/// capped by the global proactive autonomy level - one cautious admin throttles the automation for
/// everybody, the same aggregation EmailActionOrchestrator applies. No admins means no one has consented
/// to automation, so the level degrades to Propose. The admin ids arrive as an unordered set, so they are
/// walked in ordinal order: without it the tie-break for DecidingAdminUserId would depend on hash order
/// and the audit would name a different admin from one run to the next for the same configuration.
/// </summary>
/// <param name="audienceResolver">Lists the admin users whose autonomy levels are aggregated.</param>
/// <param name="autonomyPreferences">Per-admin autonomy level rows; a missing row falls back to AutonomyDefaults.DefaultLevel.</param>
/// <param name="governanceResolver">Source of the installation-wide autonomy cap.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class NextPeriodAutonomyResolver : INextPeriodAutonomyResolver
{
    private const AutonomyLevel NoAdminsLevel = AutonomyLevel.Propose;

    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly IAgentAutonomyPreferenceRepository _autonomyPreferences;
    private readonly IProactiveGovernanceResolver _governanceResolver;

    public NextPeriodAutonomyResolver(
        IPlanningAudienceResolver audienceResolver,
        IAgentAutonomyPreferenceRepository autonomyPreferences,
        IProactiveGovernanceResolver governanceResolver)
    {
        _audienceResolver = audienceResolver;
        _autonomyPreferences = autonomyPreferences;
        _governanceResolver = governanceResolver;
    }

    public async Task<NextPeriodAutonomyDecision> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var adminIds = await _audienceResolver.GetAdminUserIdsAsync(cancellationToken);
        if (adminIds.Count == 0)
        {
            return new NextPeriodAutonomyDecision(NoAdminsLevel, null);
        }

        var minimum = AutonomyLevel.FullyAutonomous;
        Guid? decidingAdminUserId = null;
        var firstAdminSeen = false;

        foreach (var adminId in adminIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            var row = await _autonomyPreferences.GetAsync(adminId, cancellationToken);
            var level = row?.Level ?? AutonomyDefaults.DefaultLevel;
            if (firstAdminSeen && level >= minimum)
            {
                continue;
            }

            firstAdminSeen = true;
            minimum = level;
            decidingAdminUserId = Guid.TryParse(adminId, out var parsed) ? parsed : null;
        }

        var globalLevel = await _governanceResolver.GetGlobalAutonomyLevelAsync(cancellationToken);

        // A global cap that bites was set by the installation, not by an admin, so nobody is named as
        // the deciding user - reporting the throttled admin there would credit a decision they did not make.
        return globalLevel < minimum
            ? new NextPeriodAutonomyDecision(globalLevel, null)
            : new NextPeriodAutonomyDecision(minimum, decidingAdminUserId);
    }
}
