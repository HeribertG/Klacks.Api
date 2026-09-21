// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IProactiveActionIdentityProvider"/>. Order matters and mirrors ScheduledTaskRunner:
/// the token is minted FIRST so every later check sees the approver's rights as they are right now,
/// not as they were when they approved. The acting UserName is
/// Klacksy's own, not the human's - the rights are borrowed, the deed is Klacksy's, and skills stamp that
/// name into CurrentUserCreated. TokenRenewalOwnerId is set because a proactive remediation is a composite
/// act that can outlive the five minutes an internal token lives; without it a long step would fail with
/// an authentication error instead of a domain one.
///
/// Two gates sit between the token and the context. First the remediation skill's own RequiredPermissions
/// are checked against the freshly expanded rights (Admin passes regardless, exactly as
/// SkillExecutorService.ValidatePermissions decides): an approver who lost the role since acknowledging
/// is refused HERE, before any claim is made, rather than three attempts later by the executor. Second the
/// unattended policy is asked as the heartbeat kind with the irreversible opt-in hard-wired to false:
/// neither an approval nor a governance rule carries per-task consent, so an irreversible skill can never
/// be executed on this path - it has to be proposed to a human instead.
///
/// Autonomy level (design 2026-09-20 §4, open question resolved): the level consulted is that of the
/// APPROVER. The approver is the person whose authority the action runs on; asking a different person's
/// preference would let an approval run past a threshold the approver never set for themselves, and
/// would bind the outcome to an account that took no part in the decision.
/// </summary>
/// <param name="tokenIssuer">Mints the short-lived internal token for the approver.</param>
/// <param name="unattendedPolicy">Fail-closed gate every unwatched skill run has to pass.</param>
/// <param name="autonomyRepository">Autonomy level of the approver; a missing row falls back to AutonomyDefaults.DefaultLevel.</param>
/// <param name="skillRegistry">Source of the skill's RequiredPermissions.</param>
/// <param name="logger">Records why an action could not be given an identity.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ProactiveActionIdentityProvider : IProactiveActionIdentityProvider
{
    private const string NoApproverReason =
        "No acting user is known for this action, so there is no identity to act under. "
        + "A remediation has to be approved by somebody who holds its permissions.";

    private const string UnknownSkillReason =
        "Skill '{0}' is not registered, so its permissions cannot be checked and nothing can run.";

    private const string PermissionsMissingReason =
        "The acting user no longer holds the permissions skill '{0}' requires ({1}). "
        + "The remediation has to be approved again by somebody who holds them.";

    private const string RefusedLogMessage =
        "Proactive action on condition {ConditionId} could not run skill {SkillName}: {Refusal} - {Reason}";

    private const string PermissionSeparator = ", ";

    // The heartbeat has no per-action consent to point at, so the opt-in that a scheduled task can carry
    // is never available here. Pinned as a constant so it cannot drift into a configurable value.
    private const bool HeartbeatAllowsIrreversible = false;

    private readonly IInternalTokenIssuer _tokenIssuer;
    private readonly IUnattendedSkillPolicy _unattendedPolicy;
    private readonly IAgentAutonomyPreferenceRepository _autonomyRepository;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ILogger<ProactiveActionIdentityProvider> _logger;

    public ProactiveActionIdentityProvider(
        IInternalTokenIssuer tokenIssuer,
        IUnattendedSkillPolicy unattendedPolicy,
        IAgentAutonomyPreferenceRepository autonomyRepository,
        ISkillRegistry skillRegistry,
        ILogger<ProactiveActionIdentityProvider> logger)
    {
        _tokenIssuer = tokenIssuer;
        _unattendedPolicy = unattendedPolicy;
        _autonomyRepository = autonomyRepository;
        _skillRegistry = skillRegistry;
        _logger = logger;
    }

    public async Task<ProactiveActionIdentity> ResolveForSkillAsync(
        Guid approverUserId,
        Guid conditionId,
        string skillName,
        CancellationToken cancellationToken = default)
    {
        if (approverUserId == Guid.Empty)
        {
            return Refuse(conditionId, skillName, ProactiveActionIdentityRefusal.NoApprover, NoApproverReason);
        }

        var userId = approverUserId;
        var token = await _tokenIssuer.IssueForOwnerAsync(userId, cancellationToken: cancellationToken);
        if (!token.Success || token.Token is null)
        {
            return Refuse(
                conditionId, skillName, ProactiveActionIdentityRefusal.TokenRefused, token.Reason ?? string.Empty);
        }

        var userPermissions = Permissions.ExpandRoles(token.Roles);
        var missingPermissions = MissingPermissions(skillName, userPermissions);
        if (missingPermissions is null)
        {
            return Refuse(
                conditionId, skillName, ProactiveActionIdentityRefusal.PermissionsMissing,
                string.Format(UnknownSkillReason, skillName));
        }

        if (missingPermissions.Count > 0)
        {
            return Refuse(
                conditionId, skillName, ProactiveActionIdentityRefusal.PermissionsMissing,
                string.Format(PermissionsMissingReason, skillName, string.Join(PermissionSeparator, missingPermissions)));
        }

        var autonomyLevel = await GetAutonomyLevelAsync(userId, cancellationToken);
        var decision = _unattendedPolicy.Decide(new UnattendedSkillRequest(
            skillName,
            userPermissions,
            autonomyLevel,
            UnattendedExecutionKind.ProactiveHeartbeat,
            HeartbeatAllowsIrreversible));

        if (!decision.Allowed)
        {
            return Refuse(
                conditionId, skillName, ProactiveActionIdentityRefusal.PolicyRefused, decision.Reason ?? string.Empty);
        }

        var context = new SkillExecutionContext
        {
            UserId = userId,
            TenantId = Guid.Empty,
            UserName = KlacksyIdentity.SystemUserName,
            UserPermissions = userPermissions,
            AccessToken = token.Token,
            TokenRenewalOwnerId = userId,
            SessionId = KlacksyIdentity.ProactiveActionSessionId(conditionId),
            BypassAutonomyGate = true
        };

        return ProactiveActionIdentity.Resolved(context, userPermissions);
    }

    /// <summary>
    /// The skill's required permissions the acting user does not hold; empty when they hold them all or
    /// are an Admin; null when the skill is unknown to the registry, which is refused rather than waved
    /// through because an unknown skill has no permission list to check against.
    /// </summary>
    private IReadOnlyList<string>? MissingPermissions(string skillName, IReadOnlyList<string> userPermissions)
    {
        var descriptor = _skillRegistry.GetSkillByName(skillName);
        if (descriptor is null)
        {
            return null;
        }

        if (userPermissions.Contains(Roles.Admin))
        {
            return Array.Empty<string>();
        }

        return descriptor.RequiredPermissions
            .Where(required => !userPermissions.Contains(required))
            .ToList();
    }

    private async Task<AutonomyLevel> GetAutonomyLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _autonomyRepository.GetAsync(userId.ToString(), cancellationToken);
        return row?.Level ?? AutonomyDefaults.DefaultLevel;
    }

    private ProactiveActionIdentity Refuse(
        Guid conditionId, string skillName, ProactiveActionIdentityRefusal refusal, string reason)
    {
        _logger.LogWarning(RefusedLogMessage, conditionId, skillName, refusal, reason);
        return ProactiveActionIdentity.Refused(refusal, reason);
    }
}
