// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Unlearns what did not earn its place. Two reasons retire an artefact and they are deliberately
/// different in kind: it has been idle for the whole pruning window, or it has been used enough times to
/// judge and did badly.
/// The idle clock starts at the activation, not at the last use. A freshly learned artefact has no last
/// use at all, and reading a missing last use as "infinitely old" would have the first pruning pass
/// delete everything the loop learned the day before.
/// A poor quote only counts once there are enough observations. Below that a single unlucky turn would
/// decide, and the loop would unlearn faster than it learns.
/// Nothing is deleted. A phrase is set to Rejected, which takes it out of the index and leaves the row
/// occupying its unique key - that row is what stops a later round from proposing the same wording
/// again. A capability is disabled rather than removed, so the trigger stops firing while the
/// composition stays readable in the card.
/// </summary>
/// <param name="artefactResolver">The activated artefacts to judge</param>
/// <param name="fitnessRepository">Latest weekly snapshot per artefact</param>
/// <param name="phraseRepository">Withdraws a learned phrase</param>
/// <param name="recipeRepository">Disables a learned capability</param>
/// <param name="clusterRepository">Moves the originating cluster to retired</param>
/// <param name="candidateRepository">Records why the candidate was retired</param>
/// <param name="optionsProvider">Settings-backed pruning window</param>
/// <param name="catalogRefresher">Rebuilt once, only when something actually changed</param>
/// <param name="timeProvider">The clock, injected so the window is testable</param>
/// <param name="logger">One line per retired artefact</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningPruner : ISkillLearningPruner
{
    private const string RefreshReason = "learned artefacts retired by pruning";

    private readonly ILearnedArtefactResolver _artefactResolver;
    private readonly ISkillLearningFitnessRepository _fitnessRepository;
    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ISkillLearningCandidateRepository _candidateRepository;
    private readonly ISkillLearningOptionsProvider _optionsProvider;
    private readonly ISkillCatalogRefresher _catalogRefresher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SkillLearningPruner> _logger;

    public SkillLearningPruner(
        ILearnedArtefactResolver artefactResolver,
        ISkillLearningFitnessRepository fitnessRepository,
        ISkillPhraseRepository phraseRepository,
        IAgentRecipeRepository recipeRepository,
        ISkillLearningClusterRepository clusterRepository,
        ISkillLearningCandidateRepository candidateRepository,
        ISkillLearningOptionsProvider optionsProvider,
        ISkillCatalogRefresher catalogRefresher,
        TimeProvider timeProvider,
        ILogger<SkillLearningPruner> logger)
    {
        _artefactResolver = artefactResolver;
        _fitnessRepository = fitnessRepository;
        _phraseRepository = phraseRepository;
        _recipeRepository = recipeRepository;
        _clusterRepository = clusterRepository;
        _candidateRepository = candidateRepository;
        _optionsProvider = optionsProvider;
        _catalogRefresher = catalogRefresher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var options = await _optionsProvider.GetAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var idleThreshold = now.AddDays(-options.PruneDays);

        var artefacts = await _artefactResolver.ListActiveAsync(
            SkillLearningDefaults.MaxArtefactsPerFitnessRun, cancellationToken);

        var retired = 0;

        foreach (var artefact in artefacts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fitness = artefact.CandidateId == null
                ? null
                : await _fitnessRepository.GetLatestAsync(artefact.CandidateId.Value, cancellationToken);

            var reason = Judge(artefact, fitness, idleThreshold, options.PruneDays);
            if (reason == null)
            {
                continue;
            }

            await RetireAsync(artefact, reason, cancellationToken);
            retired++;
        }

        if (retired > 0)
        {
            await _catalogRefresher.RefreshAsync(RefreshReason, cancellationToken);
            _logger.LogInformation("Skill learning pruning retired {Count} artefact(s)", retired);
        }

        return retired;
    }

    private static string? Judge(
        LearnedArtefact artefact, SkillLearningFitness? fitness, DateTime idleThreshold, int pruneDays)
    {
        var lastUsed = fitness?.LastUsedAtUtc ?? artefact.ActivatedAtUtc;
        if (lastUsed < idleThreshold)
        {
            return $"Nothing used it in {pruneDays} days.";
        }

        if (fitness == null || fitness.Uses < SkillLearningDefaults.PruneMinUsesForQuote)
        {
            return null;
        }

        return fitness.Quote < SkillLearningDefaults.PruneMinQuote
            ? $"It helped in only {fitness.Quote:P0} of {fitness.Uses} uses."
            : null;
    }

    private async Task RetireAsync(
        LearnedArtefact artefact, string reason, CancellationToken cancellationToken)
    {
        if (string.Equals(artefact.Kind, SkillLearningOutcomeKinds.Capability, StringComparison.Ordinal))
        {
            await DisableRecipeAsync(artefact.OwnerName, cancellationToken);
        }
        else if (artefact.PhraseId != null)
        {
            await _phraseRepository.SetStatusAsync(
                artefact.PhraseId.Value, SkillPhraseStatuses.Rejected, cancellationToken);
        }

        if (artefact.CandidateId != null)
        {
            await _candidateRepository.RetireAsync(artefact.CandidateId.Value, reason, cancellationToken);
        }

        await _clusterRepository.FinishRetirementAsync(artefact.ClusterId, reason, cancellationToken);

        _logger.LogInformation(
            "Retired learned {Kind} '{Owner}': {Reason}", artefact.Kind, artefact.OwnerName, reason);
    }

    private async Task DisableRecipeAsync(string recipeName, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByNameAsync(recipeName, cancellationToken);
        if (recipe == null || !recipe.IsEnabled)
        {
            return;
        }

        recipe.IsEnabled = false;
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);
    }
}
