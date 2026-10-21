// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides under which identity and token the steps of a plan run: the self-reflection identity a resumed goal plan
/// keeps, and the owner token a background plan renews before every step.
/// </summary>
/// <param name="internalTokenIssuer">Mints owner tokens for the resume identity and for the per-step renewal</param>
/// <param name="logger">The plan executor's logger, so fallback and renewal warnings keep its category</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanStepIdentityResolver
{
    private readonly IInternalTokenIssuer _internalTokenIssuer;
    private readonly ILogger _logger;

    public PlanStepIdentityResolver(IInternalTokenIssuer internalTokenIssuer, ILogger logger)
    {
        _internalTokenIssuer = internalTokenIssuer;
        _logger = logger;
    }

    /// <summary>
    /// Resumes a self-reflection plan under its owner's identity rather than under whoever pressed resume. The remaining
    /// steps were authorized by the same approved GoalCandidate that let GoalPlanExecutionService start the plan unattended,
    /// so they keep running under GoalSelfReflectionAuditConstants.AuditUserName, the candidate's SessionId and the owner as
    /// TokenRenewalOwnerId; otherwise a human clearing a Sensitive-step pause would silently take over the audit trail for the
    /// automation's own remaining steps. The rights come from a freshly minted token carrying the owner's CURRENT roles - the
    /// frozen permission CSV is no longer the source of truth, only the marker that an approved candidate exists. UserId and
    /// TenantId stay as supplied (UserId still feeds the autonomy-level fallback). A UserGoal plan always keeps the supplied
    /// context; a missing or unapproved candidate, an empty CSV, a non-Guid owner or a refused token fall back to the resuming
    /// caller with a log - the same fail-closed stance as PlanAutonomyLevelResolver. Only consulted on ApproveAndContinueAsync;
    /// ExecutePlanAsync always runs under the context GoalPlanExecutionService already built.
    /// </summary>
    /// <param name="plan">The plan being resumed</param>
    /// <param name="goalCandidate">The candidate that produced it, when the origin is self-reflection</param>
    /// <param name="suppliedContext">Context of the caller that triggered the resume</param>
    public async Task<SkillExecutionContext> ResolveResumeContextAsync(
        AgentPlan plan,
        GoalCandidate? goalCandidate,
        SkillExecutionContext suppliedContext,
        CancellationToken cancellationToken)
    {
        if (plan.Origin != AgentPlanOrigin.SelfReflection)
        {
            return suppliedContext;
        }

        if (goalCandidate?.Status != GoalCandidateStatus.Approved ||
            string.IsNullOrWhiteSpace(goalCandidate.OwnerPermissionsCsv))
        {
            _logger.LogWarning(
                "Plan {PlanId} resumed under origin {Origin} but has no approved goal candidate with frozen " +
                "permissions; continuing under the resuming caller's context instead of the self-reflection identity",
                plan.Id, plan.Origin);
            return suppliedContext;
        }

        if (!Guid.TryParse(goalCandidate.UserId, out var ownerUserId))
        {
            _logger.LogWarning(
                "Plan {PlanId} resumed but goal candidate owner '{UserId}' is not a Guid; continuing under " +
                "the resuming caller's context",
                plan.Id, goalCandidate.UserId);
            return suppliedContext;
        }

        var token = await _internalTokenIssuer.IssueForOwnerAsync(ownerUserId, cancellationToken: cancellationToken);
        if (!token.Success)
        {
            _logger.LogWarning(
                "Plan {PlanId} resumed but no token could be issued for its owner: {Reason}; continuing under " +
                "the resuming caller's context",
                plan.Id, token.Reason);
            return suppliedContext;
        }

        return suppliedContext with
        {
            UserName = GoalSelfReflectionAuditConstants.AuditUserName,
            UserPermissions = Permissions.ExpandRoles(token.Roles),
            AccessToken = token.Token,
            TokenRenewalOwnerId = ownerUserId,
            SessionId = GoalSelfReflectionAuditConstants.SessionIdPrefix + goalCandidate.Id
        };
    }

    /// <summary>
    /// Mints a fresh token before each step of a background plan. The internal token lives only minutes,
    /// while a plan step can involve an LLM call or a whole wizard run — without this the later steps of
    /// a long plan would fail with an authentication error instead of a domain one, and the plan record
    /// would say something misleading. Interactive plans are left alone: their token belongs to the user
    /// sitting in front of it and must never be silently swapped.
    /// </summary>
    /// <param name="stepContext">Context the next step would run under</param>
    /// <param name="planId">Only for the log line when minting fails</param>
    public async Task<SkillExecutionContext> RefreshBackgroundTokenAsync(
        SkillExecutionContext stepContext, Guid planId, CancellationToken cancellationToken)
    {
        if (stepContext.TokenRenewalOwnerId is not { } ownerUserId)
        {
            return stepContext;
        }

        var token = await _internalTokenIssuer.IssueForOwnerAsync(ownerUserId, stepContext.ExternalAgentAccessMode.HasValue ? Roles.Authorised : null, cancellationToken);
        if (!token.Success)
        {
            _logger.LogWarning(
                "Plan {PlanId} could not renew its token between steps: {Reason}; the remaining steps run " +
                "with the previous one and will fail once it expires",
                planId, token.Reason);
            return stepContext;
        }

        return stepContext with
        {
            AccessToken = token.Token,
            UserPermissions = Permissions.ExpandRoles(token.Roles)
        };
    }
}
