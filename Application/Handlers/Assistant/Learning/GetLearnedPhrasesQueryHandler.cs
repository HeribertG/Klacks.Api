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
/// <param name="artefactResolver">Links a learned phrase back to the candidate its snapshots hang off</param>
/// <param name="fitnessRepository">Latest usefulness snapshot per artefact</param>

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class GetLearnedPhrasesQueryHandler
    : BaseHandler, IRequestHandler<GetLearnedPhrasesQuery, IReadOnlyList<LearnedPhraseDto>>
{
    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly ILearnedArtefactResolver _artefactResolver;
    private readonly ISkillLearningFitnessRepository _fitnessRepository;

    public GetLearnedPhrasesQueryHandler(
        ISkillPhraseRepository phraseRepository,
        IProposedSkillChangeRepository proposalRepository,
        ILearnedArtefactResolver artefactResolver,
        ISkillLearningFitnessRepository fitnessRepository,
        ILogger<GetLearnedPhrasesQueryHandler> logger)
        : base(logger)
    {
        _phraseRepository = phraseRepository;
        _proposalRepository = proposalRepository;
        _artefactResolver = artefactResolver;
        _fitnessRepository = fitnessRepository;
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

                var fitness = await ResolveFitnessAsync(request.Limit, cancellationToken);

                var rows = learned
                    .Select(phrase => new LearnedPhraseDto(
                        phrase.Id,
                        LearnedPhraseSources.Learned,
                        LearnedPhraseStatuses.Active,
                        phrase.OwnerName,
                        phrase.Language,
                        phrase.Phrase,
                        phrase.CreateTime,
                        Snapshot(fitness, phrase.Id)?.Quote,
                        Snapshot(fitness, phrase.Id)?.Uses))
                    .Concat(proposals.Select(proposal => new LearnedPhraseDto(
                        proposal.Id,
                        LearnedPhraseSources.Description,
                        proposal.Status,
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

    // Keyed by phrase id rather than by owner: the card lists one row per wording, and the phrase id is
    // the only handle the client already has. Several wordings for the same skill therefore show the
    // same figures, which is what the attribution can honestly support - the capture records which
    // skill's phrase occurred, not which wording did.
    private async Task<IReadOnlyDictionary<Guid, SkillLearningFitness>> ResolveFitnessAsync(
        int limit, CancellationToken cancellationToken)
    {
        var artefacts = (await _artefactResolver.ListActiveAsync(limit, cancellationToken))
            .Where(a => a.PhraseId != null && a.CandidateId != null)
            .ToList();

        if (artefacts.Count == 0)
        {
            return new Dictionary<Guid, SkillLearningFitness>();
        }

        var byCandidate = await _fitnessRepository.GetLatestForCandidatesAsync(
            [.. artefacts.Select(a => a.CandidateId!.Value)], cancellationToken);

        var byPhrase = new Dictionary<Guid, SkillLearningFitness>();

        foreach (var artefact in artefacts)
        {
            if (byCandidate.TryGetValue(artefact.CandidateId!.Value, out var snapshot))
            {
                byPhrase[artefact.PhraseId!.Value] = snapshot;
            }
        }

        return byPhrase;
    }

    private static SkillLearningFitness? Snapshot(
        IReadOnlyDictionary<Guid, SkillLearningFitness> fitness, Guid phraseId) =>
        fitness.TryGetValue(phraseId, out var found) ? found : null;
}
