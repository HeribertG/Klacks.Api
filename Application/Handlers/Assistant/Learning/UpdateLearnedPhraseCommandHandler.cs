// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Edits one row of the "phrasings" section. The id alone says which store is meant: the two id spaces
/// are disjoint Guids, so the handler probes the phrase store first and falls back to the proposal store,
/// rather than trusting a source discriminator the client sends. A learned phrase whose new text already
/// exists for the same owner and language is a conflict, not an error - the partial unique index rejects
/// it and the card shows the collision.
/// After a phrase changed, the skill catalogue is refreshed so the knowledge index picks the new wording
/// up; a description proposal is only edited here, never applied - applying it stays the approval flow's job.
/// </summary>
/// <param name="phraseRepository">Learned trigger phrases</param>
/// <param name="proposalRepository">Proposed description changes</param>
/// <param name="catalogRefresher">Rebuilds the skill catalogue and the knowledge index after a phrase change</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class UpdateLearnedPhraseCommandHandler
    : IRequestHandler<UpdateLearnedPhraseCommand, LearningMutationResult>
{
    public const int MinPhraseLength = 3;
    public const int MinDescriptionLength = 10;
    public const int MaxDescriptionLength = 500;

    private const string RefreshReason = "learned phrase edited by an administrator";

    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly ISkillCatalogRefresher _catalogRefresher;

    public UpdateLearnedPhraseCommandHandler(
        ISkillPhraseRepository phraseRepository,
        IProposedSkillChangeRepository proposalRepository,
        ISkillCatalogRefresher catalogRefresher)
    {
        _phraseRepository = phraseRepository;
        _proposalRepository = proposalRepository;
        _catalogRefresher = catalogRefresher;
    }

    public async Task<LearningMutationResult> Handle(
        UpdateLearnedPhraseCommand request, CancellationToken cancellationToken)
    {
        var phrase = await _phraseRepository.GetByIdAsync(request.Id, cancellationToken);
        if (phrase != null)
        {
            return await UpdatePhraseAsync(request, cancellationToken);
        }

        var proposal = await _proposalRepository.GetByIdAsync(request.Id, cancellationToken);
        if (proposal == null)
        {
            return LearningMutationResult.NotFound();
        }

        var description = request.Description?.Trim();
        if (string.IsNullOrEmpty(description)
            || description.Length < MinDescriptionLength
            || description.Length > MaxDescriptionLength)
        {
            return LearningMutationResult.Invalid(
                $"Description must be between {MinDescriptionLength} and {MaxDescriptionLength} characters.");
        }

        proposal.ValueAfter = description;
        proposal.UpdateTime = DateTime.UtcNow;
        await _proposalRepository.UpdateAsync(proposal, cancellationToken);

        return LearningMutationResult.Success();
    }

    private async Task<LearningMutationResult> UpdatePhraseAsync(
        UpdateLearnedPhraseCommand request, CancellationToken cancellationToken)
    {
        var text = request.Phrase?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length < MinPhraseLength)
        {
            return LearningMutationResult.Invalid($"Phrase must be at least {MinPhraseLength} characters.");
        }

        if (!await _phraseRepository.TryUpdatePhraseTextAsync(request.Id, text, cancellationToken))
        {
            return LearningMutationResult.Duplicate();
        }

        await _catalogRefresher.RefreshAsync(RefreshReason, cancellationToken);
        return LearningMutationResult.Success();
    }
}
