// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IProactiveActionIdentityProvider"/>. Order matters and mirrors ScheduledTaskRunner:
/// the token is minted FIRST so the policy sees the owner's rights as they are right now, not as they
/// were when somebody wrote the governance rule. The acting UserName is Klacksy's own, not the owner's -
/// the rights are borrowed, the deed is Klacksy's, and skills stamp that name into CurrentUserCreated.
/// TokenRenewalOwnerId is set because a proactive remediation is a composite act that can outlive the
/// five minutes an internal token lives; without it a long step would fail with an authentication error
/// instead of a domain one.
/// </summary>
/// <param name="tokenIssuer">Mints the short-lived internal token for the responsible owner.</param>
/// <param name="unattendedPolicy">Fail-closed gate every unwatched skill run has to pass.</param>
/// <param name="logger">Records why an action could not be given an identity.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ProactiveActionIdentityProvider : IProactiveActionIdentityProvider
{
    private const string NoResponsibleOwnerReason =
        "No responsible owner is configured for this trigger kind, so there is no identity to act under. "
        + "Set one in the proactive governance settings.";

    private const string RefusedLogMessage =
        "Proactive action on condition {ConditionId} could not run skill {SkillName}: {Refusal} - {Reason}";

    private readonly IInternalTokenIssuer _tokenIssuer;
    private readonly IUnattendedSkillPolicy _unattendedPolicy;
    private readonly ILogger<ProactiveActionIdentityProvider> _logger;

    public ProactiveActionIdentityProvider(
        IInternalTokenIssuer tokenIssuer,
        IUnattendedSkillPolicy unattendedPolicy,
        ILogger<ProactiveActionIdentityProvider> logger)
    {
        _tokenIssuer = tokenIssuer;
        _unattendedPolicy = unattendedPolicy;
        _logger = logger;
    }

    public async Task<ProactiveActionIdentity> ResolveForSkillAsync(
        Guid? responsibleOwnerUserId,
        Guid conditionId,
        string skillName,
        CancellationToken cancellationToken = default)
    {
        if (responsibleOwnerUserId is not Guid ownerUserId || ownerUserId == Guid.Empty)
        {
            return Refuse(
                conditionId, skillName, ProactiveActionIdentityRefusal.NoResponsibleOwner, NoResponsibleOwnerReason);
        }

        var token = await _tokenIssuer.IssueForOwnerAsync(ownerUserId, cancellationToken: cancellationToken);
        if (!token.Success || token.Token is null)
        {
            return Refuse(
                conditionId, skillName, ProactiveActionIdentityRefusal.TokenRefused, token.Reason ?? string.Empty);
        }

        var userPermissions = Permissions.ExpandRoles(token.Roles);
        var decision = _unattendedPolicy.Decide(skillName, userPermissions);
        if (!decision.Allowed)
        {
            return Refuse(
                conditionId, skillName, ProactiveActionIdentityRefusal.PolicyRefused, decision.Reason ?? string.Empty);
        }

        var context = new SkillExecutionContext
        {
            UserId = ownerUserId,
            TenantId = Guid.Empty,
            UserName = KlacksyIdentity.SystemUserName,
            UserPermissions = userPermissions,
            AccessToken = token.Token,
            TokenRenewalOwnerId = ownerUserId,
            SessionId = KlacksyIdentity.ProactiveActionSessionId(conditionId),
            BypassAutonomyGate = true
        };

        return ProactiveActionIdentity.Resolved(context, userPermissions);
    }

    private ProactiveActionIdentity Refuse(
        Guid conditionId, string skillName, ProactiveActionIdentityRefusal refusal, string reason)
    {
        _logger.LogWarning(RefusedLogMessage, conditionId, skillName, refusal, reason);
        return ProactiveActionIdentity.Refused(refusal, reason);
    }
}
