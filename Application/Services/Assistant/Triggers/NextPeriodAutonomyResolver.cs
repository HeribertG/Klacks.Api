// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default INextPeriodAutonomyResolver. Folds the SAME four brakes every other autonomous path
/// respects into one decision: the global kill switch, the installation-wide autonomy level and the
/// Enabled/MaxAction pair of the next_period_scheduling_due governance row - all three of which arrive
/// through IProactiveGovernanceResolver.ResolveAsync - plus the minimum autonomy level over all admin
/// users. One cautious admin throttles the automation for everybody, and governance can only lower the
/// result, never raise it. No admins means no one has consented to automation, so the level degrades
/// to Propose.
/// The raw global level is read a second time on purpose instead of being taken from the governance
/// decision's GlobalAutonomyCap: that cap maps Autonomous and FullyAutonomous onto the same
/// ProactiveMaxAction.Execute, so deriving the level from it would let an installation on level 2
/// auto-commit, which only level 3 may do.
/// </summary>
/// <param name="adminAutonomy">Minimum autonomy level over all admins and the admin who holds it.</param>
/// <param name="governanceResolver">Kill switch, global level and the governance row of this kind.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class NextPeriodAutonomyResolver : INextPeriodAutonomyResolver
{
    private const AutonomyLevel NoAdminsLevel = AutonomyLevel.Propose;
    private const AutonomyLevel AutofillMinimumLevel = AutonomyLevel.Autonomous;
    private const AutonomyLevel CommitMinimumLevel = AutonomyLevel.FullyAutonomous;
    private const ProactiveMaxAction AutofillMinimumAction = ProactiveMaxAction.Prepare;
    private const ProactiveMaxAction CommitMinimumAction = ProactiveMaxAction.Execute;

    private readonly IAdminAutonomyLevelAggregator _adminAutonomy;
    private readonly IProactiveGovernanceResolver _governanceResolver;

    public NextPeriodAutonomyResolver(
        IAdminAutonomyLevelAggregator adminAutonomy,
        IProactiveGovernanceResolver governanceResolver)
    {
        _adminAutonomy = adminAutonomy;
        _governanceResolver = governanceResolver;
    }

    public async Task<NextPeriodAutonomyDecision> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var governance = await _governanceResolver.ResolveAsync(
            AgentTriggerKinds.NextPeriodSchedulingDue, groupId: null, cancellationToken);
        var aggregate = await _adminAutonomy.AggregateAsync(
            AdminAutonomyMissingPreferencePolicy.FallBackToDefault, cancellationToken);

        var (effectiveLevel, decidingAdminUserId) = await CapByGlobalLevelAsync(aggregate, cancellationToken);

        var canStartAutofill = governance is { KillSwitchActive: false, Enabled: true }
            && governance.EffectiveMaxAction >= AutofillMinimumAction
            && effectiveLevel >= AutofillMinimumLevel;
        var blockedBy = ResolveBlockedBy(governance, effectiveLevel);

        return new NextPeriodAutonomyDecision(
            effectiveLevel,
            decidingAdminUserId,
            canStartAutofill,
            blockedBy == NextPeriodAutonomyBlockedBy.None,
            blockedBy);
    }

    /// <summary>
    /// A global cap that bites was set by the installation, not by an admin, so nobody is named as the
    /// deciding user - reporting the throttled admin there would credit a decision they did not make.
    /// </summary>
    private async Task<(AutonomyLevel Level, Guid? DecidingAdminUserId)> CapByGlobalLevelAsync(
        AdminAutonomyAggregate aggregate, CancellationToken cancellationToken)
    {
        var globalLevel = await _governanceResolver.GetGlobalAutonomyLevelAsync(cancellationToken);
        if (aggregate.MinimumLevel is not { } adminMinimum)
        {
            return (NoAdminsLevel, null);
        }

        return globalLevel < adminMinimum
            ? (globalLevel, null)
            : (adminMinimum, aggregate.DecidingAdminUserId);
    }

    private static NextPeriodAutonomyBlockedBy ResolveBlockedBy(
        ProactiveGovernanceDecision governance, AutonomyLevel effectiveLevel)
    {
        if (governance.KillSwitchActive)
        {
            return NextPeriodAutonomyBlockedBy.KillSwitch;
        }

        if (!governance.Enabled)
        {
            return NextPeriodAutonomyBlockedBy.KindDisabled;
        }

        if (governance.EffectiveMaxAction < CommitMinimumAction)
        {
            return NextPeriodAutonomyBlockedBy.MaxAction;
        }

        return effectiveLevel < CommitMinimumLevel
            ? NextPeriodAutonomyBlockedBy.AutonomyLevel
            : NextPeriodAutonomyBlockedBy.None;
    }
}
