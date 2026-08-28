// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Agent C — Skill-Description-Optimizer. Reads recent trajectories where the user flagged the chosen
/// skill as wrong, then asks the cheapest enabled LLM model to suggest a tighter description for that
/// skill so future queries of the same shape stop matching it. Each suggestion is persisted as a
/// pending ProposedSkillChange. NEVER mutates the live skill — approval happens via a separate command.
/// Every correction is spent once: the trajectories a proposal was built from are stamped with a
/// consumption watermark, so the same handful of corrections cannot justify a fresh narrowing on every
/// later run. Together with the "already applied automatically" check that is what keeps the sharpening
/// from turning its own ratchet.
/// Before a proposal is written, the corrected target of each piece of evidence is frozen as a golden
/// case. Such a case is red at the moment it is frozen — the excerpt reaches the wrong skill, which is
/// the whole complaint — so it never blocks the sharpening it came from; it holds the improvement in
/// place once the narrowing worked, because every later round must keep that excerpt on its target.
/// </summary>
/// <param name="trajectoryRepository">Repository to fetch recent unconsumed corrected trajectories</param>
/// <param name="proposalRepository">Repository for proposed changes</param>
/// <param name="agentSkillRepository">Used to look up the current skill description</param>
/// <param name="caseRepository">Resolves the skill a correction named as the intended target</param>
/// <param name="goldenCaseRepository">Where the intended target is frozen as a regression expectation</param>
/// <param name="providerFactory">Factory to obtain an LLM provider</param>
/// <param name="llmRepository">Used to find the cheapest enabled model</param>
/// <param name="logger">Logger</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public class SkillDescriptionOptimizer : ISkillDescriptionOptimizer
{
    private const int OptimizerMaxTokens = 256;
    private const int MaxEvidencePerProposal = 5;
    private const double OptimizerTemperature = 0.2;
    private const int MinDescriptionLength = 10;
    private const int MaxDescriptionLength = 500;

    private static readonly string SystemPrompt =
        "You tighten skill descriptions for an assistant that uses semantic search over descriptions to pick the right skill. " +
        "Given a user query that the assistant misclassified into the WRONG skill, rewrite the description of that wrong skill " +
        "so it would NOT match queries of that shape next time, while still describing what the skill actually does. " +
        "Constraints: keep it under 400 characters, English, factual, no marketing fluff, no examples in the description, " +
        "do not invent capabilities the skill does not have. " +
        "Respond ONLY with a JSON object: {\"description\":\"...\",\"justification\":\"...\"}.";

    private readonly ISkillSelectionTrajectoryRepository _trajectoryRepository;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillLearningCaseRepository _caseRepository;
    private readonly ISkillLearningGoldenCaseRepository _goldenCaseRepository;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _llmRepository;
    private readonly ILogger<SkillDescriptionOptimizer> _logger;

    public SkillDescriptionOptimizer(
        ISkillSelectionTrajectoryRepository trajectoryRepository,
        IProposedSkillChangeRepository proposalRepository,
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        ISkillLearningCaseRepository caseRepository,
        ISkillLearningGoldenCaseRepository goldenCaseRepository,
        ILLMProviderFactory providerFactory,
        ILLMRepository llmRepository,
        ILogger<SkillDescriptionOptimizer> logger)
    {
        _trajectoryRepository = trajectoryRepository;
        _proposalRepository = proposalRepository;
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _caseRepository = caseRepository;
        _goldenCaseRepository = goldenCaseRepository;
        _providerFactory = providerFactory;
        _llmRepository = llmRepository;
        _logger = logger;
    }

    public async Task<int> GenerateProposalsAsync(int maxTrajectoriesToAnalyze, CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            _logger.LogInformation("No default agent — Agent C skipped");
            return 0;
        }

        var trajectories = await _trajectoryRepository.GetUncorrectedWrongSkillAsync(agent.Id, maxTrajectoriesToAnalyze, cancellationToken);
        var wrongSkillCases = trajectories
            .Where(t => !string.IsNullOrWhiteSpace(t.LlmChosenSkill))
            .ToList();

        if (wrongSkillCases.Count == 0)
        {
            _logger.LogInformation("No wrong-skill corrections to analyze");
            return 0;
        }

        var groups = wrongSkillCases
            .GroupBy(t => t.LlmChosenSkill!, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var (model, provider) = await GetCheapestModelAndProviderAsync();
        if (model == null || provider == null)
        {
            _logger.LogWarning("No enabled LLM model/provider available — Agent C skipped");
            return 0;
        }

        var generated = 0;
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var skill = await _agentSkillRepository.GetByNameAsync(agent.Id, group.Key, cancellationToken);
            if (skill == null) continue;

            if (await _proposalRepository.HasOpenProposalForSkillAsync(skill.Id, ProposedChangeFields.Description, cancellationToken))
            {
                _logger.LogDebug("Skill {Name} already has a pending proposal — skip", skill.Name);
                continue;
            }

            var evidence = group.Take(MaxEvidencePerProposal).ToList();
            var examples = evidence.Select(t => t.IntentExcerpt).ToList();
            var suggestion = await GenerateSuggestionAsync(skill, examples, model, provider, cancellationToken);
            if (suggestion == null) continue;

            await FreezeIntendedTargetsAsync(evidence, skill.Name, cancellationToken);

            var proposal = new ProposedSkillChange
            {
                Id = Guid.NewGuid(),
                AgentId = skill.AgentId,
                SkillId = skill.Id,
                SkillName = skill.Name,
                Field = ProposedChangeFields.Description,
                ValueBefore = skill.Description,
                ValueAfter = suggestion.Description,
                Justification = suggestion.Justification,
                Status = ProposedChangeStatuses.Pending,
                EvidenceJson = JsonSerializer.Serialize(examples),
                CreateTime = DateTime.UtcNow
            };

            await _proposalRepository.AddAsync(proposal, cancellationToken);
            await _trajectoryRepository.MarkSharpenedAsync(
                [.. evidence.Select(t => t.Id)], DateTime.UtcNow, cancellationToken);
            generated++;
        }

        _logger.LogInformation("Agent C generated {Count} pending proposals from {Cases} wrong-skill corrections",
            generated, wrongSkillCases.Count);
        return generated;
    }

    // Only a correction that named where the wish belonged can be frozen. An implicit correction names
    // nothing, and a correction pointing back at the skill being narrowed contradicts itself - freezing
    // either would hand the gate an expectation the sharpening is designed to break.
    private async Task FreezeIntendedTargetsAsync(
        IReadOnlyList<SkillSelectionTrajectory> evidence,
        string wronglyChosenSkill,
        CancellationToken cancellationToken)
    {
        foreach (var trajectory in evidence)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(trajectory.IntentExcerpt))
            {
                continue;
            }

            var expected = await _caseRepository.FindExpectedSkillByTrajectoryAsync(
                trajectory.Id, cancellationToken);

            if (string.IsNullOrWhiteSpace(expected)
                || string.Equals(expected, wronglyChosenSkill, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (await _goldenCaseRepository.ExistsAsync(trajectory.IntentExcerpt, expected, cancellationToken))
            {
                continue;
            }

            await _goldenCaseRepository.AddAsync(
                new SkillLearningGoldenCase
                {
                    Id = Guid.NewGuid(),
                    Query = trajectory.IntentExcerpt,
                    Locale = trajectory.Locale,
                    ExpectedSourceId = expected
                },
                cancellationToken);
        }
    }

    private async Task<Suggestion?> GenerateSuggestionAsync(
        AgentSkill skill,
        IReadOnlyList<string> examples,
        LLMModel model,
        ILLMProvider provider,
        CancellationToken cancellationToken)
    {
        var examplesBlock = string.Join("\n", examples.Select(e => $"- {e}"));
        var userMessage =
            $"Skill name: {skill.Name}\n" +
            $"Current description: {skill.Description}\n" +
            $"Examples of user queries that this skill was WRONGLY chosen for:\n{examplesBlock}\n\n" +
            "Suggest a tighter description.";

        var request = new LLMProviderRequest
        {
            Message = userMessage,
            SystemPrompt = SystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = OptimizerTemperature,
            MaxTokens = OptimizerMaxTokens,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken
        };

        try
        {
            var response = await provider.ProcessAsync(request);
            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogDebug("Optimizer LLM call returned no content for skill {Name}", skill.Name);
                return null;
            }

            return ParseSuggestion(response.Content, skill.Description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Optimizer LLM call failed for skill {Name}", skill.Name);
            return null;
        }
    }

    private Suggestion? ParseSuggestion(string content, string currentDescription)
    {
        try
        {
            var trimmed = ExtractJsonObject(content);
            if (string.IsNullOrWhiteSpace(trimmed)) return null;

            using var doc = JsonDocument.Parse(trimmed);
            var description = doc.RootElement.TryGetProperty("description", out var d) ? d.GetString() : null;
            var justification = doc.RootElement.TryGetProperty("justification", out var j) ? j.GetString() : string.Empty;

            if (string.IsNullOrWhiteSpace(description)) return null;
            description = description.Trim();
            if (description.Length < MinDescriptionLength || description.Length > MaxDescriptionLength) return null;
            if (string.Equals(description, currentDescription.Trim(), StringComparison.OrdinalIgnoreCase)) return null;

            return new Suggestion(description, (justification ?? string.Empty).Trim());
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Optimizer suggestion JSON parse failed");
            return null;
        }
    }

    private static string? ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        return content.Substring(start, end - start + 1);
    }

    private async Task<(LLMModel? model, ILLMProvider? provider)> GetCheapestModelAndProviderAsync()
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);
        var cheapest = models.OrderBy(m => m.CostPerInputToken + m.CostPerOutputToken).FirstOrDefault();
        if (cheapest == null) return (null, null);
        var provider = await _providerFactory.GetProviderForModelAsync(cheapest.ModelId);
        return (cheapest, provider);
    }

    private sealed record Suggestion(string Description, string Justification);
}
