// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// See ISkillRerankThresholdTuner. Reads the recent turns of the default agent, keeps those that can teach
/// something (SkillRerankOutcomeReader), lets SkillRerankThresholdCalculator pick the next threshold and writes it
/// into the live threshold and the settings table. Nothing happens unless the skill rerank runs in Adaptive mode:
/// in Always or Never the threshold is not used, so there is nothing to learn and nothing to restore.
/// </summary>
/// <param name="threshold">The live threshold every skill pass reads</param>
/// <param name="settingsRepository">Where the learned value survives a restart</param>
/// <param name="unitOfWork">Commits the staged setting</param>
/// <param name="trajectoryRepository">Source of the captured turns</param>
/// <param name="agentRepository">Resolves the default agent whose turns are read</param>
/// <param name="logger">Records every change of the threshold</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using SettingsKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillRerankThresholdTuner : ISkillRerankThresholdTuner
{
    private readonly ISkillRerankThreshold _threshold;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISkillSelectionTrajectoryRepository _trajectoryRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ILogger<SkillRerankThresholdTuner> _logger;

    public SkillRerankThresholdTuner(
        ISkillRerankThreshold threshold,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISkillSelectionTrajectoryRepository trajectoryRepository,
        IAgentRepository agentRepository,
        ILogger<SkillRerankThresholdTuner> logger)
    {
        _threshold = threshold;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _trajectoryRepository = trajectoryRepository;
        _agentRepository = agentRepository;
        _logger = logger;
    }

    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        if (!_threshold.IsLearned)
        {
            return;
        }

        var stored = (await _settingsRepository.GetSettingNoTracking(SettingsKeys.KLACKSY_SKILL_RERANK_MARGIN))?.Value;
        if (double.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value))
        {
            _threshold.Set(value);
            _logger.LogInformation("Skill rerank threshold restored: {Threshold}", _threshold.Current);
        }
    }

    public async Task<double?> TuneAsync(CancellationToken cancellationToken = default)
    {
        if (!_threshold.IsLearned)
        {
            return null;
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return null;
        }

        var since = DateTime.UtcNow.AddDays(-SkillRerankTuningDefaults.WindowDays);
        var trajectories = await _trajectoryRepository.GetRecentAsync(
            agent.Id, SkillRerankTuningDefaults.MaxTrajectories, cancellationToken);
        var outcomes = trajectories
            .Where(t => t.CreateTime == null || t.CreateTime >= since)
            .Select(SkillRerankOutcomeReader.Read)
            .OfType<SkillRerankOutcome>()
            .ToList();

        var current = _threshold.Current;
        var next = SkillRerankThresholdCalculator.Next(
            outcomes, current,
            KnowledgeIndexConstants.MinSkillRerankMarginThreshold,
            KnowledgeIndexConstants.MaxSkillRerankMarginThreshold);
        if (next == null)
        {
            _logger.LogInformation(
                "Skill rerank threshold unchanged at {Threshold}: {Count} usable turns are not enough yet",
                current, outcomes.Count);
            return null;
        }

        _threshold.Set(next.Value);
        await _settingsRepository.UpsertSettingAsync(
            SettingsKeys.KLACKSY_SKILL_RERANK_MARGIN, next.Value.ToString(CultureInfo.InvariantCulture));
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation(
            "Skill rerank threshold tuned from {Previous} to {Threshold} on {Count} usable turns",
            current, next.Value, outcomes.Count);
        return next;
    }
}
