// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Moves gate-passed description proposals to exported once the export script wrote them into skill-seeds.json,
/// recording who did it. Every other id is reported back as skipped and left untouched, so a repeated or stale
/// request cannot rewrite a verdict.
/// </summary>
/// <param name="proposalRepository">Loads and saves the proposals (self-committing)</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class MarkProposalsExportedCommandHandler : IRequestHandler<MarkProposalsExportedCommand, MarkProposalsExportedResult>
{
    private readonly IProposedSkillChangeRepository _proposalRepository;

    public MarkProposalsExportedCommandHandler(IProposedSkillChangeRepository proposalRepository)
    {
        _proposalRepository = proposalRepository;
    }

    public async Task<MarkProposalsExportedResult> Handle(
        MarkProposalsExportedCommand request, CancellationToken cancellationToken)
    {
        var marked = 0;
        var skipped = new List<Guid>();

        foreach (var id in request.ProposalIds.Distinct())
        {
            var proposal = await _proposalRepository.GetByIdAsync(id, cancellationToken);
            if (proposal == null
                || proposal.Status != ProposedChangeStatuses.GatePassed
                || proposal.Field != ProposedChangeFields.Description)
            {
                skipped.Add(id);
                continue;
            }

            var now = DateTime.UtcNow;
            proposal.Status = ProposedChangeStatuses.Exported;
            proposal.ReviewedBy = request.ReviewedBy;
            proposal.ReviewedAt = now;
            proposal.UpdateTime = now;
            await _proposalRepository.UpdateAsync(proposal, cancellationToken);
            marked++;
        }

        return new MarkProposalsExportedResult(marked, skipped);
    }
}
