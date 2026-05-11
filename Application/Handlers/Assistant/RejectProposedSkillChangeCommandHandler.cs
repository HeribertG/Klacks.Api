// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks a pending proposed skill change as rejected without touching the live skill.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class RejectProposedSkillChangeCommandHandler : IRequestHandler<RejectProposedSkillChangeCommand, RejectProposedSkillChangeResult>
{
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly ILogger<RejectProposedSkillChangeCommandHandler> _logger;

    public RejectProposedSkillChangeCommandHandler(
        IProposedSkillChangeRepository proposalRepository,
        ILogger<RejectProposedSkillChangeCommandHandler> logger)
    {
        _proposalRepository = proposalRepository;
        _logger = logger;
    }

    public async Task<RejectProposedSkillChangeResult> Handle(RejectProposedSkillChangeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewedBy))
        {
            return new RejectProposedSkillChangeResult(false, "ReviewedBy is required.");
        }

        var proposal = await _proposalRepository.GetByIdAsync(request.ProposalId, cancellationToken);
        if (proposal == null)
        {
            return new RejectProposedSkillChangeResult(false, "Proposal not found.");
        }

        if (proposal.Status != ProposedChangeStatuses.Pending)
        {
            return new RejectProposedSkillChangeResult(false, $"Proposal is in status '{proposal.Status}', cannot reject.");
        }

        proposal.Status = ProposedChangeStatuses.Rejected;
        proposal.ReviewedBy = request.ReviewedBy;
        proposal.ReviewedAt = DateTime.UtcNow;
        proposal.UpdateTime = DateTime.UtcNow;

        await _proposalRepository.UpdateAsync(proposal, cancellationToken);

        _logger.LogInformation("Proposal {ProposalId} rejected by {ReviewedBy}", proposal.Id, request.ReviewedBy);
        return new RejectProposedSkillChangeResult(true, null);
    }
}
