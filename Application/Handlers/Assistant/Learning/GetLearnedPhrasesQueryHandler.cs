// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the "phrasings" section of the learning card from two stores at once: the phrases the loop
/// itself learned (skill_phrase, Source=Learned) and the sharpened descriptions the description optimizer
/// proposed (proposed_skill_changes). They are merged because an administrator judges both the same way -
/// "does this wording help Klacksy find the right skill". Approved and rejected proposals are history and
/// stay out; pending, automatically applied and regression-blocked ones are the ones still worth looking at.
/// </summary>
/// <param name="phraseRepository">Learned trigger phrases</param>
/// <param name="proposalRepository">Proposed description changes</param>

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class GetLearnedPhrasesQueryHandler
    : BaseHandler, IRequestHandler<GetLearnedPhrasesQuery, IReadOnlyList<LearnedPhraseDto>>
{
    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly IProposedSkillChangeRepository _proposalRepository;

    public GetLearnedPhrasesQueryHandler(
        ISkillPhraseRepository phraseRepository,
        IProposedSkillChangeRepository proposalRepository,
        ILogger<GetLearnedPhrasesQueryHandler> logger)
        : base(logger)
    {
        _phraseRepository = phraseRepository;
        _proposalRepository = proposalRepository;
    }

    public async Task<IReadOnlyList<LearnedPhraseDto>> Handle(
        GetLearnedPhrasesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            async () =>
            {
                var learned = await _phraseRepository.GetActiveBySourceAsync(
                    SkillPhraseSources.Learned, request.Limit, cancellationToken);

                var proposals = await _proposalRepository.GetByStatusesAsync(
                    ProposedChangeStatuses.ReviewableForLearning, request.Limit, cancellationToken);

                var rows = learned
                    .Select(phrase => new LearnedPhraseDto(
                        phrase.Id,
                        LearnedPhraseSources.Learned,
                        phrase.OwnerName,
                        phrase.Language,
                        phrase.Phrase,
                        phrase.CreateTime,
                        null,
                        null))
                    .Concat(proposals.Select(proposal => new LearnedPhraseDto(
                        proposal.Id,
                        LearnedPhraseSources.Description,
                        proposal.SkillName,
                        string.Empty,
                        proposal.ValueAfter,
                        proposal.CreateTime,
                        null,
                        null)))
                    .OrderByDescending(row => row.LearnedAt)
                    .Take(request.Limit)
                    .ToList();

                return (IReadOnlyList<LearnedPhraseDto>)rows;
            },
            "get learned phrases",
            new { request.Limit });
    }
}
