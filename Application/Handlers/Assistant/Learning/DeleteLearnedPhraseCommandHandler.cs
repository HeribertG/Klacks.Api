// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Withdraws one row of the "phrasings" section. Nothing is erased: a learned phrase goes to Rejected and
/// a proposal goes to rejected, both of which keep the row as a record that this wording was tried and
/// discarded. That record is what stops a later learning round from proposing the very same phrase again.
/// </summary>
/// <param name="phraseRepository">Learned trigger phrases</param>
/// <param name="proposalRepository">Proposed description changes</param>
/// <param name="catalogRefresher">Rebuilds the skill catalogue so a withdrawn phrase stops influencing retrieval</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class DeleteLearnedPhraseCommandHandler
    : IRequestHandler<DeleteLearnedPhraseCommand, LearningMutationResult>
{
    private const string RefreshReason = "learned phrase withdrawn by an administrator";

    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly ISkillCatalogRefresher _catalogRefresher;

    public DeleteLearnedPhraseCommandHandler(
        ISkillPhraseRepository phraseRepository,
        IProposedSkillChangeRepository proposalRepository,
        ISkillCatalogRefresher catalogRefresher)
    {
        _phraseRepository = phraseRepository;
        _proposalRepository = proposalRepository;
        _catalogRefresher = catalogRefresher;
    }

    public async Task<LearningMutationResult> Handle(
        DeleteLearnedPhraseCommand request, CancellationToken cancellationToken)
    {
        if (await _phraseRepository.SetStatusAsync(request.Id, SkillPhraseStatuses.Rejected, cancellationToken))
        {
            await _catalogRefresher.RefreshAsync(RefreshReason, cancellationToken);
            return LearningMutationResult.Success();
        }

        var proposal = await _proposalRepository.GetByIdAsync(request.Id, cancellationToken);
        if (proposal == null)
        {
            return LearningMutationResult.NotFound();
        }

        proposal.Status = ProposedChangeStatuses.Rejected;
        proposal.ReviewedAt = DateTime.UtcNow;
        proposal.UpdateTime = DateTime.UtcNow;
        await _proposalRepository.UpdateAsync(proposal, cancellationToken);

        return LearningMutationResult.Success();
    }
}
