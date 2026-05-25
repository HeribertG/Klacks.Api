// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Probes every configured LLM model with a small function-calling task that mirrors Klacksy's
/// core requirement: the model must reliably emit a structured tool call (not prose) when asked
/// to perform an action. A model qualifies for Klacksy when it is reachable, returns the expected
/// function call and offers a context window large enough for Klacksy's system prompt and skills.
/// </summary>
public sealed class KlacksyModelCheckService
{
    private const int ProbeMaxTokens = 256;
    private const int MinContextWindowForKlacksy = 32000;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(30);

    private const string ProbeFunctionName = "create_employee";
    private const string FirstNameParameter = "firstName";
    private const string LastNameParameter = "lastName";
    private const string ExpectedFirstName = "Anna";
    private const string ExpectedLastName = "Müller";

    private const string ProbeSystemPrompt =
        "You are Klacksy, an assistant that performs actions by calling functions. " +
        "When the user asks you to do something, you MUST respond with the matching function call " +
        "and its arguments. Never answer with prose when a function is available.";
    private const string ProbeUserMessage =
        "Bitte lege einen neuen Mitarbeiter namens Anna Müller an.";

    private readonly ILLMRepository _llmRepository;
    private readonly LLMProviderOrchestrator _orchestrator;
    private readonly ILogger<KlacksyModelCheckService> _logger;

    public KlacksyModelCheckService(
        ILLMRepository llmRepository,
        LLMProviderOrchestrator orchestrator,
        ILogger<KlacksyModelCheckService> logger)
    {
        _llmRepository = llmRepository;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<KlacksyModelCheckResult>> CheckAllAsync(CancellationToken cancellationToken)
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: false);
        if (models.Count == 0)
        {
            return [];
        }

        var results = new List<KlacksyModelCheckResult>(models.Count);
        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await CheckModelAsync(model, cancellationToken));
        }

        return Sort(results);
    }

    private async Task<KlacksyModelCheckResult> CheckModelAsync(LLMModel model, CancellationToken cancellationToken)
    {
        var displayName = string.IsNullOrWhiteSpace(model.ModelName) ? model.ModelId : model.ModelName;
        try
        {
            var probe = await ProbeAsync(model.ModelId, cancellationToken);
            var qualifies = probe.IsHealthy
                && probe.SupportsToolCalling
                && model.ContextWindow >= MinContextWindowForKlacksy;
            return Build(model, displayName, probe.IsHealthy, probe.SupportsToolCalling, probe.LatencyMs, qualifies, probe.Error);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Klacksy model check threw for {ModelId}", model.ModelId);
            return Build(model, displayName, isHealthy: false, supportsToolCalling: false, latencyMs: 0, qualifies: false, error: ex.Message);
        }
    }

    private static KlacksyModelCheckResult Build(
        LLMModel model,
        string displayName,
        bool isHealthy,
        bool supportsToolCalling,
        long latencyMs,
        bool qualifies,
        string? error) =>
        new(
            ModelId: model.ModelId,
            DisplayName: displayName,
            ProviderId: model.ProviderId,
            IsHealthy: isHealthy,
            SupportsToolCalling: supportsToolCalling,
            LatencyMs: latencyMs,
            ContextWindow: model.ContextWindow,
            CostPerInputToken: model.CostPerInputToken,
            CostPerOutputToken: model.CostPerOutputToken,
            Qualifies: qualifies,
            Error: error);

    private async Task<(bool IsHealthy, bool SupportsToolCalling, long LatencyMs, string? Error)> ProbeAsync(
        string modelId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(modelId);
        if (error is not null || model is null || provider is null)
        {
            stopwatch.Stop();
            return (false, false, stopwatch.ElapsedMilliseconds, error ?? "LLM provider unavailable.");
        }

        var request = new LLMProviderRequest
        {
            Message = ProbeUserMessage,
            SystemPrompt = ProbeSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [BuildProbeFunction()],
            Temperature = 0.0,
            MaxTokens = ProbeMaxTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ProbeTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(request, cts.Token);
            stopwatch.Stop();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return (false, false, stopwatch.ElapsedMilliseconds, $"Probe timed out after {ProbeTimeout.TotalSeconds:F0}s.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Klacksy model probe threw for {ModelId}", modelId);
            return (false, false, stopwatch.ElapsedMilliseconds, $"Probe failed: {ex.Message}");
        }

        if (!response.Success)
        {
            return (false, false, stopwatch.ElapsedMilliseconds, response.Error ?? "Provider rejected the probe.");
        }

        var supportsToolCalling = EmittedExpectedCall(response);
        var probeError = supportsToolCalling ? null : "Model did not emit the expected function call.";
        return (true, supportsToolCalling, stopwatch.ElapsedMilliseconds, probeError);
    }

    private static LLMFunction BuildProbeFunction() =>
        new()
        {
            Name = ProbeFunctionName,
            Description = "Creates a new employee record in the Klacks system.",
            Parameters = new Dictionary<string, object>
            {
                [FirstNameParameter] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Given name of the new employee.",
                },
                [LastNameParameter] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Family name of the new employee.",
                },
            },
            RequiredParameters = [FirstNameParameter, LastNameParameter],
        };

    private static bool EmittedExpectedCall(LLMProviderResponse response)
    {
        var call = response.FunctionCalls
            .FirstOrDefault(c => string.Equals(c.FunctionName, ProbeFunctionName, StringComparison.OrdinalIgnoreCase));
        if (call is null)
        {
            return false;
        }

        return ParameterContains(call.Parameters, FirstNameParameter, ExpectedFirstName)
            && ParameterContains(call.Parameters, LastNameParameter, ExpectedLastName);
    }

    private static bool ParameterContains(IReadOnlyDictionary<string, object> parameters, string key, string expected)
    {
        if (!parameters.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        var text = value.ToString() ?? string.Empty;
        return text.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static List<KlacksyModelCheckResult> Sort(IEnumerable<KlacksyModelCheckResult> results) =>
        results
            .OrderByDescending(r => r.Qualifies)
            .ThenByDescending(r => r.SupportsToolCalling)
            .ThenBy(r => r.CostPerInputToken + r.CostPerOutputToken)
            .ThenBy(r => r.LatencyMs)
            .ThenByDescending(r => r.ContextWindow)
            .ToList();
}
