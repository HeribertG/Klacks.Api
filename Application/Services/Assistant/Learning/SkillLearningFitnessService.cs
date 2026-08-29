// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Oracle O3. Measures whether what the loop learned is doing any good, once per activated artefact and
/// calendar week, over a rolling thirty-day window.
/// Two things it counts are worth naming. The first is the thumbs-up: without it the whole measurement
/// would consist of negatives, and an artefact nobody complained about would look exactly like an
/// artefact that helped. The second is recurrence - a new case in the very cluster the artefact was
/// supposed to close. That is the only negative signal that needs nobody to say anything: the user
/// simply asked again and was refused again.
/// The row is keyed by artefact and week and rewritten on every pass, so a week in progress carries a
/// running figure rather than whatever the first pass of that week happened to see. An artefact whose
/// candidate row is gone is measured for the card but not stored, because the snapshot table hangs off
/// the candidate.
/// </summary>
/// <param name="artefactResolver">The activated artefacts to measure</param>
/// <param name="trajectoryRepository">Turn-level counters per artefact</param>
/// <param name="caseRepository">Recurrences of the originating cluster</param>
/// <param name="fitnessRepository">Where the weekly snapshot is written</param>
/// <param name="timeProvider">The clock, injected so the window and the week boundary are testable</param>
/// <param name="logger">One summary line per pass</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningFitnessService : ISkillLearningFitnessService
{
    private const decimal MaxQuote = 1m;

    private readonly ILearnedArtefactResolver _artefactResolver;
    private readonly ISkillSelectionTrajectoryRepository _trajectoryRepository;
    private readonly ISkillLearningCaseRepository _caseRepository;
    private readonly ISkillLearningFitnessRepository _fitnessRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SkillLearningFitnessService> _logger;

    public SkillLearningFitnessService(
        ILearnedArtefactResolver artefactResolver,
        ISkillSelectionTrajectoryRepository trajectoryRepository,
        ISkillLearningCaseRepository caseRepository,
        ISkillLearningFitnessRepository fitnessRepository,
        TimeProvider timeProvider,
        ILogger<SkillLearningFitnessService> logger)
    {
        _artefactResolver = artefactResolver;
        _trajectoryRepository = trajectoryRepository;
        _caseRepository = caseRepository;
        _fitnessRepository = fitnessRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.AddDays(-SkillLearningDefaults.FitnessWindowDays);
        var weekStart = StartOfWeek(now);

        var artefacts = await _artefactResolver.ListActiveAsync(
            SkillLearningDefaults.MaxArtefactsPerFitnessRun, cancellationToken);

        var measured = 0;

        foreach (var artefact in artefacts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (artefact.CandidateId == null)
            {
                continue;
            }

            var snapshot = await MeasureAsync(artefact, windowStart, cancellationToken);
            snapshot.Id = Guid.NewGuid();
            snapshot.CandidateId = artefact.CandidateId.Value;
            snapshot.WindowStartUtc = weekStart;

            await _fitnessRepository.UpsertAsync(snapshot, cancellationToken);
            measured++;
        }

        if (measured > 0)
        {
            _logger.LogInformation("Skill learning fitness measured {Count} artefact(s)", measured);
        }

        return measured;
    }

    private async Task<SkillLearningFitness> MeasureAsync(
        LearnedArtefact artefact, DateTime windowStart, CancellationToken cancellationToken)
    {
        var usage = string.Equals(artefact.Kind, SkillLearningOutcomeKinds.Capability, StringComparison.Ordinal)
            ? await _trajectoryRepository.CountRecipeUsageAsync(artefact.OwnerName, windowStart, cancellationToken)
            : await _trajectoryRepository.CountPhraseUsageAsync(artefact.OwnerName, windowStart, cancellationToken);

        // Only occurrences after the activation count: everything the cluster collected before is the
        // evidence that justified learning in the first place, not a verdict on the result.
        var recurrenceFloor = artefact.ActivatedAtUtc > windowStart ? artefact.ActivatedAtUtc : windowStart;
        var recurrences = await _caseRepository.CountSinceAsync(
            artefact.ClusterId, recurrenceFloor, cancellationToken);

        return new SkillLearningFitness
        {
            Uses = usage.Uses,
            Successes = usage.Successes,
            Failures = usage.Corrections + recurrences,
            Helpful = usage.Helpful,
            Corrections = usage.Corrections,
            Recurrences = recurrences,
            LastUsedAtUtc = usage.LastUsedAtUtc,
            Quote = Quote(usage)
        };
    }

    // A turn can be both a success and a thumbs-up, so the numerator can exceed the denominator. The
    // result is capped rather than left above one: this is read as a quote, and a quote of 1.4 would
    // mean nothing to anybody looking at the card.
    private static decimal Quote(LearnedArtefactUsage usage)
    {
        if (usage.Uses == 0)
        {
            return 0m;
        }

        var quote = (decimal)(usage.Successes + usage.Helpful) / usage.Uses;
        return quote > MaxQuote ? MaxQuote : quote;
    }

    private static DateTime StartOfWeek(DateTime moment)
    {
        var date = moment.Date;
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return DateTime.SpecifyKind(date.AddDays(-offset), DateTimeKind.Utc);
    }
}
