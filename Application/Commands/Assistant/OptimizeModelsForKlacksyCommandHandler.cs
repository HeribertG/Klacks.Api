// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Commands.Assistant;

/// <summary>
/// Runs the Klacksy readiness probe, aligns each model's enabled state with the probe verdict and
/// guarantees a usable default: a previously chosen default is kept when it still qualifies,
/// otherwise the best qualifying model becomes the default.
/// </summary>
public class OptimizeModelsForKlacksyCommandHandler
    : IRequestHandler<OptimizeModelsForKlacksyCommand, KlacksyModelCheckResponse>
{
    private readonly KlacksyModelCheckService _checkService;
    private readonly ILLMRepository _repository;
    private readonly ILogger<OptimizeModelsForKlacksyCommandHandler> _logger;

    public OptimizeModelsForKlacksyCommandHandler(
        KlacksyModelCheckService checkService,
        ILLMRepository repository,
        ILogger<OptimizeModelsForKlacksyCommandHandler> logger)
    {
        _checkService = checkService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<KlacksyModelCheckResponse> Handle(
        OptimizeModelsForKlacksyCommand request,
        CancellationToken cancellationToken)
    {
        var results = await _checkService.CheckAllAsync(cancellationToken);

        foreach (var result in results)
        {
            await ApplyEnabledStateAsync(result.ModelId, result.Qualifies);
        }

        var defaultModelId = await ResolveDefaultAsync(results);

        var dtos = results
            .Select(r => new KlacksyModelCheckDto(
                r.ModelId,
                r.DisplayName,
                r.ProviderId,
                r.IsHealthy,
                r.SupportsToolCalling,
                r.LatencyMs,
                r.ContextWindow,
                r.CostPerInputToken,
                r.CostPerOutputToken,
                r.Qualifies,
                IsEnabled: r.Qualifies,
                IsDefault: string.Equals(r.ModelId, defaultModelId, StringComparison.Ordinal),
                r.Error))
            .ToArray();

        _logger.LogInformation(
            "Klacksy model optimization: {Qualified}/{Total} models qualified, default is {DefaultModelId}",
            results.Count(r => r.Qualifies),
            results.Count,
            defaultModelId ?? "(none)");

        return new KlacksyModelCheckResponse(dtos, defaultModelId);
    }

    private async Task ApplyEnabledStateAsync(string modelId, bool shouldBeEnabled)
    {
        var model = await _repository.GetModelByIdAsync(modelId);
        if (model is null || model.IsEnabled == shouldBeEnabled)
        {
            return;
        }

        model.IsEnabled = shouldBeEnabled;
        await _repository.UpdateModelAsync(model);
    }

    private async Task<string?> ResolveDefaultAsync(IReadOnlyList<KlacksyModelCheckResult> results)
    {
        var bestQualifying = results.FirstOrDefault(r => r.Qualifies);
        if (bestQualifying is null)
        {
            return null;
        }

        var currentDefault = await _repository.GetDefaultModelAsync();
        var currentDefaultQualifies = currentDefault is not null
            && results.Any(r => r.Qualifies
                && string.Equals(r.ModelId, currentDefault.ModelId, StringComparison.Ordinal));

        if (currentDefaultQualifies)
        {
            return currentDefault!.ModelId;
        }

        await _repository.SetDefaultModelAsync(bestQualifying.ModelId);
        return bestQualifying.ModelId;
    }
}
