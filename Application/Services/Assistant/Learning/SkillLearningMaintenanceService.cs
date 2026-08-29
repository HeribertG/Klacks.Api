// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The housekeeping half of the learning loop: promotes clusters that reached the threshold, retires the
/// finished ones that outlived the retention window - retired, dismissed and unfulfillable alike, because
/// all three are finished business for the admin card even though only the first two are terminal in the
/// state machine - and then measures and prunes what the loop has already activated. Contains no learning
/// and calls no language model; the learning itself runs beside it in the same background service.
/// The promotion sweep is a backstop, not the primary path: the collector already promotes a cluster the
/// moment it crosses the threshold, and this catches the cases where two concurrent turns each computed
/// a stale counter.
/// </summary>
/// <param name="clusterRepository">Cluster store</param>
/// <param name="fitnessService">Oracle O3, refreshes the weekly usefulness snapshots</param>
/// <param name="pruner">Withdraws artefacts that went unused or proved unhelpful</param>
/// <param name="optionsProvider">Settings-backed thresholds</param>
/// <param name="logger">Run summary</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningMaintenanceService : ISkillLearningMaintenanceService
{
    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ISkillLearningFitnessService _fitnessService;
    private readonly ISkillLearningPruner _pruner;
    private readonly ISkillLearningOptionsProvider _optionsProvider;
    private readonly ILogger<SkillLearningMaintenanceService> _logger;

    public SkillLearningMaintenanceService(
        ISkillLearningClusterRepository clusterRepository,
        ISkillLearningFitnessService fitnessService,
        ISkillLearningPruner pruner,
        ISkillLearningOptionsProvider optionsProvider,
        ILogger<SkillLearningMaintenanceService> logger)
    {
        _clusterRepository = clusterRepository;
        _fitnessService = fitnessService;
        _pruner = pruner;
        _optionsProvider = optionsProvider;
        _logger = logger;
    }

    public async Task<SkillLearningMaintenanceResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var options = await _optionsProvider.GetAsync(cancellationToken);

        var promoted = await _clusterRepository.PromoteReadyAsync(
            options.MinOccurrences, options.MinDistinctUsers, cancellationToken);

        var retired = await _clusterRepository.SoftDeleteRetentionEligibleOlderThanAsync(
            DateTime.UtcNow.AddDays(-options.RetentionDays), cancellationToken);

        // Measure first, then prune: the pruner reads the snapshot the measurement just wrote, so a
        // decision is never taken on numbers that are a whole tick out of date. Both are idempotent -
        // the snapshot is keyed by artefact and week, and a retired artefact is no longer listed as
        // active - so running them on every six-hourly tick rather than exactly daily is harmless.
        var measured = await _fitnessService.RunAsync(cancellationToken);
        var pruned = await _pruner.RunAsync(cancellationToken);

        if (promoted > 0 || retired > 0 || pruned > 0)
        {
            _logger.LogInformation(
                "Skill learning maintenance: promoted {Promoted} cluster(s), retired {Retired}, "
                + "measured {Measured} artefact(s), pruned {Pruned}",
                promoted, retired, measured, pruned);
        }

        return new SkillLearningMaintenanceResult(promoted, retired, measured, pruned);
    }
}
