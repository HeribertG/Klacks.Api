// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IPeriodAutoCloseResolver, modelled on NextPeriodAutonomyResolver but stricter, because a seal
/// is not cleanly reversible and fires the payroll export. ALL of the following must hold, otherwise the
/// decision is blocked with the strongest brake:
/// the global kill switch is off; the governance rule of period_auto_close (the group's own rule when one
/// exists, otherwise the installation-wide one) is enabled and its effective MaxAction is Execute; the raw
/// installation-wide proactive autonomy level is FullyAutonomous; and the minimum autonomy level over all
/// admins is FullyAutonomous, with a usable deciding admin id.
///
/// Governance ceiling decision: the gate reads the rule of the dedicated kind period_auto_close - the kind
/// under which this path reports its outcomes, exactly as the next-period path reads its own kind - and NOT
/// the rule of period_close_due, whose MaxAction steers the reminder. period_auto_close has no seeded
/// override (ProactiveGovernanceDefaults.SeededMaxActionOverrides), so every installation starts at the
/// fail-safe Hint and an administrator has to raise exactly this rule to Execute to opt in. That rule is the
/// explicit, revocable consent for unattended closing; the global level and the admin preferences alone never
/// suffice.
///
/// Two deliberate differences to the next-period resolver: the raw global level is compared against
/// FullyAutonomous instead of trusting the governance decision's GlobalAutonomyCap (that cap maps Autonomous
/// and FullyAutonomous onto the same Execute and would let level 2 close periods), and admins without a
/// stored autonomy row BLOCK the aggregation (AdminAutonomyMissingPreferencePolicy.Block, the unattended
/// precedent of GoalPlanExecutionService) instead of counting as the shared default - nobody consented to a
/// default.
/// </summary>
/// <param name="adminAutonomy">Minimum autonomy level over all admin users and the admin who holds it.</param>
/// <param name="governanceResolver">Kill switch, global level and the period_auto_close governance rule.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class PeriodAutoCloseResolver : IPeriodAutoCloseResolver
{
    private const AutonomyLevel RequiredLevel = AutonomyLevel.FullyAutonomous;
    private const AutonomyLevel NoAdminLevel = AutonomyLevel.Propose;
    private const ProactiveMaxAction RequiredAction = ProactiveMaxAction.Execute;

    private readonly IAdminAutonomyLevelAggregator _adminAutonomy;
    private readonly IProactiveGovernanceResolver _governanceResolver;

    public PeriodAutoCloseResolver(
        IAdminAutonomyLevelAggregator adminAutonomy,
        IProactiveGovernanceResolver governanceResolver)
    {
        _adminAutonomy = adminAutonomy;
        _governanceResolver = governanceResolver;
    }

    public async Task<PeriodAutoCloseDecision> ResolveAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var governance = await _governanceResolver.ResolveAsync(
            AgentTriggerKinds.PeriodAutoClose, groupId, cancellationToken);
        var globalLevel = await _governanceResolver.GetGlobalAutonomyLevelAsync(cancellationToken);
        var aggregate = await _adminAutonomy.AggregateAsync(
            AdminAutonomyMissingPreferencePolicy.Block, cancellationToken);

        var adminLevel = aggregate.MinimumLevel ?? NoAdminLevel;
        var effectiveLevel = globalLevel < adminLevel ? globalLevel : adminLevel;
        var blockedBy = ResolveBlockedBy(governance, globalLevel, aggregate);

        return new PeriodAutoCloseDecision(
            effectiveLevel,
            aggregate.DecidingAdminUserId,
            blockedBy == PeriodAutoCloseBlockedBy.None,
            blockedBy);
    }

    private static PeriodAutoCloseBlockedBy ResolveBlockedBy(
        ProactiveGovernanceDecision governance,
        AutonomyLevel globalLevel,
        AdminAutonomyAggregate aggregate)
    {
        if (governance.KillSwitchActive)
        {
            return PeriodAutoCloseBlockedBy.KillSwitch;
        }

        if (!governance.Enabled)
        {
            return PeriodAutoCloseBlockedBy.KindDisabled;
        }

        if (governance.EffectiveMaxAction < RequiredAction)
        {
            return PeriodAutoCloseBlockedBy.MaxAction;
        }

        if (globalLevel != RequiredLevel)
        {
            return PeriodAutoCloseBlockedBy.GlobalLevel;
        }

        if (aggregate.MinimumLevel is not { } adminMinimum)
        {
            return aggregate.AdminWithoutStoredLevel is null
                ? PeriodAutoCloseBlockedBy.NoAdmins
                : PeriodAutoCloseBlockedBy.AdminLevelMissing;
        }

        if (adminMinimum != RequiredLevel)
        {
            return PeriodAutoCloseBlockedBy.AdminLevel;
        }

        return aggregate.DecidingAdminUserId is { } adminId && adminId != Guid.Empty
            ? PeriodAutoCloseBlockedBy.None
            : PeriodAutoCloseBlockedBy.NoDecidingAdmin;
    }
}
