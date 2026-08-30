// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies a skill description change an administrator approved by hand: writes the new value to the
/// AgentSkill row, increments the version, marks the proposal as approved, invalidates the skill cache,
/// reloads the SkillRegistry and triggers a KnowledgeIndex re-sync so the new description reaches the
/// semantic search.
/// Only a proposal the routing regression gate blocked can be approved here. A pending one belongs to the
/// learning loop, which applies it automatically once the gate is green; letting a person approve it in
/// parallel would make two writers for one transition, and the earlier of them would silently lose. This
/// path is therefore an override of the gate, and its answer to a pending proposal is "not yet".
/// The outcome is a LearningMutationResult like every other mutation on the learning card: a proposal or
/// skill that is not there is a 404, a stale description auto-reject is a 409, everything else refusing
/// is a 400.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ApproveProposedSkillChangeCommandHandler : IRequestHandler<ApproveProposedSkillChangeCommand, LearningMutationResult>
{
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillCatalogRefresher _skillCatalogRefresher;
    private readonly ILogger<ApproveProposedSkillChangeCommandHandler> _logger;

    public ApproveProposedSkillChangeCommandHandler(
        IProposedSkillChangeRepository proposalRepository,
        IAgentSkillRepository agentSkillRepository,
        ISkillCatalogRefresher skillCatalogRefresher,
        ILogger<ApproveProposedSkillChangeCommandHandler> logger)
    {
        _proposalRepository = proposalRepository;
        _agentSkillRepository = agentSkillRepository;
        _skillCatalogRefresher = skillCatalogRefresher;
        _logger = logger;
    }

    public async Task<LearningMutationResult> Handle(ApproveProposedSkillChangeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewedBy))
        {
            return LearningMutationResult.Invalid("ReviewedBy is required.");
        }

        var proposal = await _proposalRepository.GetByIdAsync(request.ProposalId, cancellationToken);
        if (proposal == null)
        {
            return LearningMutationResult.NotFound();
        }

        if (proposal.Status != ProposedChangeStatuses.BlockedRegression)
        {
            return LearningMutationResult.Invalid(
                $"Proposal is in status '{proposal.Status}', only a regression-blocked proposal can be approved by hand.");
        }

        if (proposal.Field != ProposedChangeFields.Description)
        {
            return LearningMutationResult.Invalid($"Field '{proposal.Field}' is not supported.");
        }

        var skill = await _agentSkillRepository.GetByIdAsync(proposal.SkillId, cancellationToken);
        if (skill == null)
        {
            return LearningMutationResult.NotFound();
        }

        if (!string.Equals(skill.Description, proposal.ValueBefore, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Skill {Name} description changed since the proposal was generated — auto-rejecting to avoid stale apply",
                skill.Name);
            proposal.Status = ProposedChangeStatuses.Rejected;
            proposal.ReviewedBy = request.ReviewedBy;
            proposal.ReviewedAt = DateTime.UtcNow;
            proposal.UpdateTime = DateTime.UtcNow;
            await _proposalRepository.UpdateAsync(proposal, cancellationToken);
            return LearningMutationResult.StaleDescription();
        }

        skill.Description = proposal.ValueAfter;
        skill.Version += 1;
        await _agentSkillRepository.UpdateAsync(skill, cancellationToken);

        proposal.Status = ProposedChangeStatuses.Approved;
        proposal.ReviewedBy = request.ReviewedBy;
        proposal.ReviewedAt = DateTime.UtcNow;
        proposal.UpdateTime = DateTime.UtcNow;
        await _proposalRepository.UpdateAsync(proposal, cancellationToken);

        await _skillCatalogRefresher.RefreshAsync($"approving proposal {proposal.Id}", cancellationToken);

        _logger.LogInformation(
            "Proposal {ProposalId} approved by {ReviewedBy}: skill {Name} description updated to v{Version}",
            proposal.Id, request.ReviewedBy, skill.Name, skill.Version);

        return LearningMutationResult.Success();
    }
}
