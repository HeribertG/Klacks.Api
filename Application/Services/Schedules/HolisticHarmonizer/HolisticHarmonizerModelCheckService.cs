// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Iterates the enabled LLM models and runs the Holistic Harmonizer pre-flight ping against each one.
/// Returns a per-model health summary so the operator can pick a model that is reachable,
/// fast and JSON-format-compliant without paying for a full Holistic Harmonizer run.
/// The vision capability check stays the gate; ranking prefers measured evidence from the
/// harmonizer-v1 eval goldset: models with a good measured composite rank first, never-evaluated
/// models second (latency heuristic) and models with a measured weak composite last.
/// </summary>
public sealed class HolisticHarmonizerModelCheckService
{
    private const decimal EvalPreferenceThreshold = 0.7m;

    private readonly ILLMRepository _llmRepository;
    private readonly IPlanProposalProvider _proposalProvider;
    private readonly IEvalRunRepository _evalRunRepository;
    private readonly ILogger<HolisticHarmonizerModelCheckService> _logger;

    public HolisticHarmonizerModelCheckService(
        ILLMRepository llmRepository,
        IPlanProposalProvider proposalProvider,
        IEvalRunRepository evalRunRepository,
        ILogger<HolisticHarmonizerModelCheckService> logger)
    {
        _llmRepository = llmRepository;
        _proposalProvider = proposalProvider;
        _evalRunRepository = evalRunRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HolisticHarmonizerModelCheckResult>> CheckAllAsync(CancellationToken cancellationToken)
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);
        if (models.Count == 0)
        {
            return [];
        }

        var evalScores = await LoadEvalScoresAsync(cancellationToken);

        var results = new List<HolisticHarmonizerModelCheckResult>(models.Count);
        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HolisticHarmonizerModelCheckResult result;
            try
            {
                var ping = await _proposalProvider.CapabilityCheckAsync(model.ModelId, cancellationToken);
                result = new HolisticHarmonizerModelCheckResult(
                    ModelId: model.ModelId,
                    DisplayName: string.IsNullOrWhiteSpace(model.ModelName) ? model.ModelId : model.ModelName,
                    ProviderId: model.ProviderId,
                    IsHealthy: ping.IsHealthy,
                    LatencyMs: ping.LatencyMs,
                    Error: ping.Error);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Model check threw for {ModelId}", model.ModelId);
                result = new HolisticHarmonizerModelCheckResult(
                    ModelId: model.ModelId,
                    DisplayName: string.IsNullOrWhiteSpace(model.ModelName) ? model.ModelId : model.ModelName,
                    ProviderId: model.ProviderId,
                    IsHealthy: false,
                    LatencyMs: 0,
                    Error: ex.Message);
            }

            if (evalScores.TryGetValue(model.ModelId, out var composite))
            {
                result = result with { EvalCompositeScore = composite };
            }

            results.Add(result);
        }

        return Sort(results);
    }

    private async Task<Dictionary<string, decimal>> LoadEvalScoresAsync(CancellationToken cancellationToken)
    {
        try
        {
            var latestRuns = await _evalRunRepository.GetLatestPerModelAsync(HarmonizerEvalGoldset.Name, cancellationToken);
            return latestRuns
                .Where(r => r.Model != null)
                .ToDictionary(r => r.Model!, r => r.CompositeScore, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loading harmonizer-eval scores failed; falling back to heuristic ranking only");
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
    }

    internal static List<HolisticHarmonizerModelCheckResult> Sort(IEnumerable<HolisticHarmonizerModelCheckResult> results) =>
        results
            .OrderByDescending(r => r.IsHealthy)
            .ThenBy(EvalEvidenceTier)
            .ThenByDescending(r => r.EvalCompositeScore ?? decimal.MinValue)
            .ThenBy(r => r.LatencyMs)
            .ToList();

    private static int EvalEvidenceTier(HolisticHarmonizerModelCheckResult result) =>
        result.EvalCompositeScore switch
        {
            null => 1,
            >= EvalPreferenceThreshold => 0,
            _ => 2
        };
}
