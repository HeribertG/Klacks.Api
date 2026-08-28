// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The housekeeping half of the learning loop: promotes clusters that reached the threshold and retires
/// terminal ones that outlived the retention window. Contains no learning and calls no language model -
/// generating artefacts is stage G2, so a run here is cheap enough to execute on every instance.
/// The promotion sweep is a backstop, not the primary path: the collector already promotes a cluster the
/// moment it crosses the threshold, and this catches the cases where two concurrent turns each computed
/// a stale counter.
/// </summary>
/// <param name="clusterRepository">Cluster store</param>
/// <param name="optionsProvider">Settings-backed thresholds</param>
/// <param name="logger">Run summary</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningMaintenanceService : ISkillLearningMaintenanceService
{
    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ISkillLearningOptionsProvider _optionsProvider;
    private readonly ILogger<SkillLearningMaintenanceService> _logger;

    public SkillLearningMaintenanceService(
        ISkillLearningClusterRepository clusterRepository,
        ISkillLearningOptionsProvider optionsProvider,
        ILogger<SkillLearningMaintenanceService> logger)
    {
        _clusterRepository = clusterRepository;
        _optionsProvider = optionsProvider;
        _logger = logger;
    }

    public async Task<SkillLearningMaintenanceResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var options = await _optionsProvider.GetAsync(cancellationToken);

        var promoted = await _clusterRepository.PromoteReadyAsync(
            options.MinOccurrences, options.MinDistinctUsers, cancellationToken);

        var retired = await _clusterRepository.SoftDeleteTerminalOlderThanAsync(
            DateTime.UtcNow.AddDays(-options.RetentionDays), cancellationToken);

        if (promoted > 0 || retired > 0)
        {
            _logger.LogInformation(
                "Skill learning maintenance: promoted {Promoted} cluster(s), retired {Retired}",
                promoted, retired);
        }

        return new SkillLearningMaintenanceResult(promoted, retired);
    }
}
