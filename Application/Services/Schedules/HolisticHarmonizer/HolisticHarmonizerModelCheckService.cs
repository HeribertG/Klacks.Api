// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Iterates the enabled LLM models and runs the Holistic Harmonizer pre-flight ping against each one.
/// Returns a per-model health summary so the operator can pick a model that is reachable,
/// fast and JSON-format-compliant without paying for a full Holistic Harmonizer run.
/// </summary>
public sealed class HolisticHarmonizerModelCheckService
{
    private readonly ILLMRepository _llmRepository;
    private readonly IPlanProposalProvider _proposalProvider;
    private readonly ILogger<HolisticHarmonizerModelCheckService> _logger;

    public HolisticHarmonizerModelCheckService(
        ILLMRepository llmRepository,
        IPlanProposalProvider proposalProvider,
        ILogger<HolisticHarmonizerModelCheckService> logger)
    {
        _llmRepository = llmRepository;
        _proposalProvider = proposalProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HolisticHarmonizerModelCheckResult>> CheckAllAsync(CancellationToken cancellationToken)
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);
        if (models.Count == 0)
        {
            return [];
        }

        var results = new List<HolisticHarmonizerModelCheckResult>(models.Count);
        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var ping = await _proposalProvider.CapabilityCheckAsync(model.ModelId, cancellationToken);
                results.Add(new HolisticHarmonizerModelCheckResult(
                    ModelId: model.ModelId,
                    DisplayName: string.IsNullOrWhiteSpace(model.ModelName) ? model.ModelId : model.ModelName,
                    ProviderId: model.ProviderId,
                    IsHealthy: ping.IsHealthy,
                    LatencyMs: ping.LatencyMs,
                    Error: ping.Error));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Model check threw for {ModelId}", model.ModelId);
                results.Add(new HolisticHarmonizerModelCheckResult(
                    ModelId: model.ModelId,
                    DisplayName: string.IsNullOrWhiteSpace(model.ModelName) ? model.ModelId : model.ModelName,
                    ProviderId: model.ProviderId,
                    IsHealthy: false,
                    LatencyMs: 0,
                    Error: ex.Message));
            }
        }

        return results;
    }
}

