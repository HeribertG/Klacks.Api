// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IGoalPlanExecutionService. Starts the unattended execution of a plan drafted from an
/// approved GoalCandidate, gated by five brakes evaluated in order (each rejection is logged with its
/// reason and returns false, never throws): the GoalReflectionExecution feature flag; the candidate
/// being Approved with a drafted PlanId; the candidate's self-assessed Confidence being exactly High
/// (anything else, including Unknown, only ever proposes — mirrors the email-analysis pipeline's
/// confidence gate, deliberately not a text keyword detector); the minimum AutonomyLevel across all
/// admin users being at least Autonomous ("the most cautious admin brakes for everyone"; no admin at
/// all, and any admin without a stored level, are treated the same as one below the floor — unlike
/// EmailActionOrchestrator.ResolveEffectiveLevelAsync this path does NOT fall back to
/// AutonomyDefaults.DefaultLevel, because that default permits autonomous work and would let a fresh
/// installation act unattended without anyone having chosen it); and the candidate's
/// OwnerPermissionsCsv being frozen and non-empty, because a background run
/// has no ClaimsPrincipal for SkillExecutorService.ValidatePermissions to read otherwise. Only after all
/// five pass is a SkillExecutionContext built — audited under a fixed, non-human user name and a
/// self-reflection SessionId so the audit trail is unambiguous — and handed to
/// IPlanChatService.StartBackgroundExecution, which runs PlanStepExecutor.ExecutePlanAsync in a fresh
/// scope. PlanStepExecutor itself resolves AutonomyLevel.FullyAutonomous for this plan because its
/// Origin is SelfReflection; this service does not set that level, it only decides whether to start the
/// plan at all.
/// </summary>
/// <param name="goalCandidateRepository">Loads the approved candidate to execute.</param>
/// <param name="audienceResolver">Resolves the admin users whose autonomy level bounds execution.</param>
/// <param name="autonomyRepository">Per-admin autonomy level; a missing row falls back to AutonomyDefaults.DefaultLevel.</param>
/// <param name="planChatService">Starts the plan running in a fresh background scope.</param>
/// <param name="options">Feature flag gating execution; see GoalReflectionExecution.</param>
/// <param name="logger">Structured log of every brake rejection and every started execution.</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Services.Assistant.Reflection;

public class GoalPlanExecutionService : IGoalPlanExecutionService
{
    private readonly IGoalCandidateRepository _goalCandidateRepository;
    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly IAgentAutonomyPreferenceRepository _autonomyRepository;
    private readonly IPlanChatService _planChatService;
    private readonly IInternalTokenIssuer _internalTokenIssuer;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<GoalPlanExecutionService> _logger;

    public GoalPlanExecutionService(
        IGoalCandidateRepository goalCandidateRepository,
        IPlanningAudienceResolver audienceResolver,
        IAgentAutonomyPreferenceRepository autonomyRepository,
        IPlanChatService planChatService,
        IInternalTokenIssuer internalTokenIssuer,
        IOptions<BackgroundServiceOptions> options,
        ILogger<GoalPlanExecutionService> logger)
    {
        _goalCandidateRepository = goalCandidateRepository;
        _audienceResolver = audienceResolver;
        _autonomyRepository = autonomyRepository;
        _planChatService = planChatService;
        _internalTokenIssuer = internalTokenIssuer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ExecuteForCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default)
    {
        if (!_options.GoalReflectionExecution)
        {
            _logger.LogInformation(
                "Goal-plan execution skipped for candidate {CandidateId}: GoalReflectionExecution flag is off",
                candidateId);
            return false;
        }

        var candidate = await _goalCandidateRepository.GetByIdAsync(candidateId, cancellationToken);
        if (candidate == null || candidate.Status != GoalCandidateStatus.Approved || candidate.PlanId is not { } planId)
        {
            _logger.LogInformation(
                "Goal-plan execution skipped for candidate {CandidateId}: not found, not approved, or no drafted plan",
                candidateId);
            return false;
        }

        if (candidate.Confidence != GoalCandidateConfidence.High)
        {
            _logger.LogInformation(
                "Goal-plan execution skipped for candidate {CandidateId}: confidence '{Confidence}' is not High " +
                "— only High executes unattended, everything else including Unknown stays a proposal",
                candidateId, candidate.Confidence);
            return false;
        }

        var minimumAdminLevel = await ResolveMinimumAdminAutonomyLevelAsync(cancellationToken);
        if (minimumAdminLevel is not { } level || level < AutonomyLevel.Autonomous)
        {
            _logger.LogInformation(
                "Goal-plan execution skipped for candidate {CandidateId}: minimum admin autonomy level is " +
                "{Level} (or no admin exists) — the most cautious admin brakes execution for everyone",
                candidateId, minimumAdminLevel?.ToString() ?? "n/a (no admin)");
            return false;
        }

        if (string.IsNullOrWhiteSpace(candidate.OwnerPermissionsCsv))
        {
            _logger.LogWarning(
                "Goal-plan execution skipped for candidate {CandidateId}: OwnerPermissionsCsv is not frozen; " +
                "without it a background run would have no permission check at all",
                candidateId);
            return false;
        }

        if (!Guid.TryParse(candidate.UserId, out var ownerUserId))
        {
            _logger.LogWarning(
                "Goal-plan execution skipped for candidate {CandidateId}: owner UserId '{UserId}' is not a Guid",
                candidateId, candidate.UserId);
            return false;
        }

        // Rights come from the owner's CURRENT roles via a freshly minted token, not from the set frozen
        // when the candidate was approved — a revoked role stops the plan on its next execution.
        var token = await _internalTokenIssuer.IssueForOwnerAsync(ownerUserId, cancellationToken: cancellationToken);
        if (!token.Success)
        {
            _logger.LogWarning(
                "Goal-plan execution skipped for candidate {CandidateId}: {Reason}", candidateId, token.Reason);
            return false;
        }

        var skillContext = new SkillExecutionContext
        {
            UserId = ownerUserId,
            TenantId = Guid.Empty,
            UserName = GoalSelfReflectionAuditConstants.AuditUserName,
            UserPermissions = Permissions.ExpandRoles(token.Roles),
            AccessToken = token.Token,
            SessionId = GoalSelfReflectionAuditConstants.SessionIdPrefix + candidateId,
            BypassAutonomyGate = true,
            SupportsUiActions = false
        };

        _planChatService.StartBackgroundExecution(planId, skillContext, resume: false);
        _logger.LogInformation(
            "Started unattended execution of plan {PlanId} for goal candidate {CandidateId}", planId, candidateId);
        return true;
    }

    private async Task<AutonomyLevel?> ResolveMinimumAdminAutonomyLevelAsync(CancellationToken cancellationToken)
    {
        var adminIds = await _audienceResolver.GetAdminUserIdsAsync(cancellationToken);
        if (adminIds.Count == 0)
        {
            return null;
        }

        var minimum = AutonomyLevel.FullyAutonomous;
        foreach (var adminId in adminIds)
        {
            var row = await _autonomyRepository.GetAsync(adminId, cancellationToken);
            if (row == null)
            {
                // Deliberately not AutonomyDefaults.DefaultLevel here: that default permits autonomous
                // work, and on a fresh installation nobody has ever chosen a level, so falling back to
                // it would let Klacksy change data unattended without any human having agreed to it.
                // Only an explicitly stored level counts for unattended goal execution. The default
                // still applies to the paths that ask a human first.
                _logger.LogInformation(
                    "Goal-plan execution braked: admin {AdminId} has no stored autonomy level, and unattended " +
                    "execution requires an explicit one",
                    adminId.ForLog());
                return null;
            }

            if (row.Level < minimum)
            {
                minimum = row.Level;
            }
        }

        return minimum;
    }
}
