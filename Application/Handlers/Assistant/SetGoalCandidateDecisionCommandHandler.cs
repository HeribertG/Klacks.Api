// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Records a human decision (approve/reject) on a goal candidate. Phase 2 is read-only: this handler
/// never drafts a plan and never executes a skill, so it takes no dependency capable of either.
/// Returns false when the decision value is not Approved/Rejected, the candidate does not exist, it
/// belongs to a different user, or it is already in a terminal status — the same candidate is never
/// decided twice. On approval the requesting user's permissions are frozen onto
/// GoalCandidate.OwnerPermissionsCsv, the same pattern ScheduledTaskRunner uses for
/// ScheduledTask.OwnerPermissionsCsv: a later, unattended plan execution has no ClaimsPrincipal, so
/// without this frozen snapshot SkillExecutorService.ValidatePermissions would have nothing to check
/// against and RBAC would be silently bypassed for self-directed goals.
/// </summary>
/// <param name="goalCandidateRepository">Persistence of GoalCandidate entities.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SetGoalCandidateDecisionCommandHandler : IRequestHandler<SetGoalCandidateDecisionCommand, bool>
{
    private readonly IGoalCandidateRepository _goalCandidateRepository;

    public SetGoalCandidateDecisionCommandHandler(IGoalCandidateRepository goalCandidateRepository)
    {
        _goalCandidateRepository = goalCandidateRepository;
    }

    public async Task<bool> Handle(SetGoalCandidateDecisionCommand request, CancellationToken cancellationToken)
    {
        if (request.Decision != GoalCandidateStatus.Approved && request.Decision != GoalCandidateStatus.Rejected)
        {
            return false;
        }

        var candidate = await _goalCandidateRepository.GetByIdAsync(request.Id, cancellationToken);
        if (candidate == null || !string.Equals(candidate.UserId, request.UserId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (GoalCandidateStatus.IsTerminal(candidate.Status))
        {
            return false;
        }

        if (request.Decision == GoalCandidateStatus.Approved)
        {
            // Security-critical: freeze the approving user's permissions now, because a later
            // unattended plan execution has no ClaimsPrincipal to read them from (see class doc).
            candidate.OwnerPermissionsCsv = string.Join(',', request.UserPermissions);
        }

        candidate.Status = request.Decision;
        candidate.DecidedUtc = DateTime.UtcNow;

        await _goalCandidateRepository.UpdateAsync(candidate, cancellationToken);
        return true;
    }
}
