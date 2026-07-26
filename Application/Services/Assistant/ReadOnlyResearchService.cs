// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs a bounded, read-only tool loop with the cheapest available model to answer analysis-style
/// questions ("analyze the month / roster") without spending the outer turn's context or premium model
/// budget. The intermediate tool results stay inside this loop; only a compact English synthesis is
/// returned. The toolset is hard-filtered to read-only skills and every tool call is re-checked against
/// that allow-list before execution, so no mutating skill can ever run here. It inherits the caller's
/// <see cref="SkillExecutionContext"/> (user id, permissions), so it can read nothing the caller cannot.
/// </summary>
/// <param name="cheapestModelResolver">Resolves the cheapest enabled model and its provider.</param>
/// <param name="skillRegistry">Source of the skills the caller is permitted to use.</param>
/// <param name="toolsetFilter">Hard read-only filter applied to the permitted skills.</param>
/// <param name="skillBridge">Executes read-only tool calls and exports skills as LLM functions.</param>

using System.Text;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Services.Assistant.Skills;
using ProviderMessage = Klacks.Api.Domain.Services.Assistant.Providers.LLMMessage;

namespace Klacks.Api.Application.Services.Assistant;

public class ReadOnlyResearchService : IReadOnlyResearchService
{
    private const string ToolChoiceAuto = "auto";
    private const string AssistantRole = "assistant";
    private const string UserRole = "user";
    private const string GatheringDataPlaceholder = "[gathering data]";
    private const string ToolNotAvailableMarker = "not available (read-only research only)";

    private readonly ICheapestModelResolver _cheapestModelResolver;
    private readonly ISkillRegistry _skillRegistry;
    private readonly IReadOnlyToolsetFilter _toolsetFilter;
    private readonly ILLMSkillBridge _skillBridge;
    private readonly ILogger<ReadOnlyResearchService> _logger;

    public ReadOnlyResearchService(
        ICheapestModelResolver cheapestModelResolver,
        ISkillRegistry skillRegistry,
        IReadOnlyToolsetFilter toolsetFilter,
        ILLMSkillBridge skillBridge,
        ILogger<ReadOnlyResearchService> logger)
    {
        _cheapestModelResolver = cheapestModelResolver;
        _skillRegistry = skillRegistry;
        _toolsetFilter = toolsetFilter;
        _skillBridge = skillBridge;
        _logger = logger;
    }

    public async Task<ReadOnlyResearchResult> ResearchAsync(
        string question,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var (model, provider) = await _cheapestModelResolver.ResolveAsync(cancellationToken);
        if (model == null || provider == null)
        {
            _logger.LogWarning("Read-only research aborted: no enabled model/provider available");
            return new ReadOnlyResearchResult(
                ReadOnlyResearchConstants.NoModelAvailableMessage, 0, 0, [], ModelAvailable: false);
        }

        var (functions, allowedNames) = BuildReadOnlyToolset(context.UserPermissions);

        var runningHistory = new List<ProviderMessage>();
        var currentMessage = question;
        var toolsUsed = new List<string>();
        var toolCallCount = 0;
        var iterationsUsed = 0;
        string lastContent = string.Empty;

        for (var iteration = 0; iteration < ReadOnlyResearchConstants.MaxIterations; iteration++)
        {
            iterationsUsed = iteration + 1;

            var response = await provider.ProcessAsync(
                BuildRequest(model, currentMessage, runningHistory, functions, forceSynthesis: false),
                cancellationToken);

            if (!response.Success)
            {
                _logger.LogWarning("Read-only research provider error on iteration {Iteration}: {Error}",
                    iterationsUsed, response.Error);
                break;
            }

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                lastContent = response.Content;
            }

            if (response.FunctionCalls.Count == 0)
            {
                return BuildResult(lastContent, iterationsUsed, toolCallCount, toolsUsed);
            }

            runningHistory.Add(new ProviderMessage { Role = UserRole, Content = currentMessage });
            runningHistory.Add(new ProviderMessage
            {
                Role = AssistantRole,
                Content = string.IsNullOrWhiteSpace(response.Content) ? GatheringDataPlaceholder : response.Content
            });

            currentMessage = await ExecuteToolCallsAsync(
                response.FunctionCalls, allowedNames, context, toolsUsed, cancellationToken);

            toolCallCount = toolsUsed.Count;

            if (toolCallCount >= ReadOnlyResearchConstants.MaxToolCalls)
            {
                _logger.LogInformation("Read-only research hit tool-call budget ({Budget})",
                    ReadOnlyResearchConstants.MaxToolCalls);
                break;
            }
        }

        var synthesis = await RunFinalSynthesisAsync(
            model, provider, currentMessage, runningHistory, lastContent, cancellationToken);

        return BuildResult(synthesis, iterationsUsed, toolCallCount, toolsUsed);
    }

    private (List<LLMFunction> Functions, HashSet<string> AllowedNames) BuildReadOnlyToolset(
        IReadOnlyList<string> userPermissions)
    {
        var permitted = _skillRegistry.GetSkillsForUser(userPermissions);
        var readOnly = _toolsetFilter.Filter(permitted, ReadOnlyResearchConstants.RunAnalysisSkillName);

        var allowedNames = readOnly
            .Select(descriptor => descriptor.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var functions = _skillBridge.GetSkillsAsLLMFunctions(userPermissions)
            .Where(function => allowedNames.Contains(function.Name))
            .ToList();

        return (functions, allowedNames);
    }

    private async Task<string> ExecuteToolCallsAsync(
        IReadOnlyList<LLMFunctionCall> calls,
        HashSet<string> allowedNames,
        SkillExecutionContext context,
        List<string> toolsUsed,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Tool Results]");

        foreach (var call in calls)
        {
            if (!allowedNames.Contains(call.FunctionName))
            {
                _logger.LogWarning(
                    "Read-only research blocked non-read-only tool call {FunctionName}", call.FunctionName);
                sb.AppendLine($"- {call.FunctionName}: {ToolNotAvailableMarker}");
                continue;
            }

            var result = await _skillBridge.ExecuteSkillFromLLMCallAsync(
                new LLMFunctionCall { FunctionName = call.FunctionName, Parameters = call.Parameters },
                context,
                cancellationToken);

            toolsUsed.Add(call.FunctionName);
            sb.AppendLine($"- {call.FunctionName}: {Cap(result.Message)}");
        }

        sb.AppendLine("[/Tool Results]");
        return sb.ToString();
    }

    private async Task<string> RunFinalSynthesisAsync(
        LLMModel model,
        ILLMProvider provider,
        string currentMessage,
        List<ProviderMessage> runningHistory,
        string fallbackContent,
        CancellationToken cancellationToken)
    {
        var response = await provider.ProcessAsync(
            BuildRequest(model, currentMessage, runningHistory, functions: [], forceSynthesis: true),
            cancellationToken);

        if (response.Success && !string.IsNullOrWhiteSpace(response.Content))
        {
            return response.Content;
        }

        return fallbackContent;
    }

    private static LLMProviderRequest BuildRequest(
        LLMModel model,
        string message,
        List<ProviderMessage> history,
        List<LLMFunction> functions,
        bool forceSynthesis)
    {
        var systemPrompt = forceSynthesis
            ? ReadOnlyResearchConstants.SystemPrompt + "\n\n" + ReadOnlyResearchConstants.SynthesisInstruction
            : ReadOnlyResearchConstants.SystemPrompt;

        return new LLMProviderRequest
        {
            Message = message,
            SystemPrompt = systemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = history,
            AvailableFunctions = functions,
            Temperature = ReadOnlyResearchConstants.Temperature,
            MaxTokens = ReadOnlyResearchConstants.MaxResponseTokens,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            ToolChoice = forceSynthesis ? null : ToolChoiceAuto
        };
    }

    private static ReadOnlyResearchResult BuildResult(
        string synthesis, int iterationsUsed, int toolCallCount, List<string> toolsUsed)
    {
        var text = string.IsNullOrWhiteSpace(synthesis)
            ? ReadOnlyResearchConstants.NoFindingsMessage
            : synthesis.Trim();

        return new ReadOnlyResearchResult(
            text,
            iterationsUsed,
            toolCallCount,
            toolsUsed.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ModelAvailable: true);
    }

    private static string Cap(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "OK";
        }

        return value.Length <= ReadOnlyResearchConstants.MaxToolResultChars
            ? value
            : value[..ReadOnlyResearchConstants.MaxToolResultChars] + "…[truncated]";
    }
}
