// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 2 PlanningAgent (autonomy-roadmap.md). Decomposes a free-text user goal into 1-7 atomic
/// skill-calls using the cheapest enabled LLM provider plus the live skill catalog. Returns an
/// AgentPlan in status 'drafting' with steps serialized as JSON. The StepExecutor (later) executes
/// each step, pairs it with the verify-skill when present, and pauses for HITL on non-reversible
/// steps.
/// </summary>
/// <param name="agentRepository">Resolves the owning agent (klacks-default today).</param>
/// <param name="skillRepository">Live skill catalog (name + short description) used as the planner's tool list.</param>
/// <param name="providerFactory">Picks the LLM provider per model.</param>
/// <param name="llmRepository">Lists enabled LLM models to find the cheapest.</param>
/// <param name="logger">Structured log of planning attempts.</param>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public class PlanningAgent : IPlanningAgent
{
    private const int MaxSteps = 7;
    private const int MaxOutputTokens = 1024;
    private const double Temperature = 0.2;
    private const int MaxSkillsInPrompt = 40;

    private static readonly HashSet<string> ReversibleSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "add_ai_memory", "add_client_to_group", "create_branch", "create_employee",
        "create_shift", "create_user", "place_work", "delete_work", "confirm_work", "unconfirm_work",
        "add_break", "add_workchange", "add_schedule_command", "approve_day", "revoke_day_approval",
        "list_scenarios", "reject_scenario", "cancel_wizard_job"
    };

    private static readonly string SystemPrompt =
        "You are Klacksy's Planning Agent. Decompose the user's goal into 1-7 atomic skill calls. " +
        "Rules:\n" +
        "1. Each step MUST reference exactly one skill name from the AVAILABLE SKILLS list.\n" +
        "2. Each step lists required parameters with concrete values, or placeholders like '$prev.id' / " +
        "   '$prev.jobId' / '$prev.scenarioId' when the value comes from the previous step's result.\n" +
        "3. Pair every mutating skill with a 'verifySkill' that READS the result, when an obvious read " +
        "   skill exists. Read-only goals don't need verify.\n" +
        "4. Set 'reversible' to true only when the step can be safely undone with another available skill.\n" +
        "5. NEVER invent skills that are not in the list. If the goal cannot be planned, respond with an " +
        "   empty steps array.\n\n" +
        "Respond ONLY with JSON of shape: " +
        "{\"steps\":[{\"order\":1,\"skill\":\"...\",\"params\":{...},\"verifySkill\":\"...\",\"reversible\":true}]}";

    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSkillRepository _skillRepository;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _llmRepository;
    private readonly ILogger<PlanningAgent> _logger;

    public PlanningAgent(
        IAgentRepository agentRepository,
        IAgentSkillRepository skillRepository,
        ILLMProviderFactory providerFactory,
        ILLMRepository llmRepository,
        ILogger<PlanningAgent> logger)
    {
        _agentRepository = agentRepository;
        _skillRepository = skillRepository;
        _providerFactory = providerFactory;
        _llmRepository = llmRepository;
        _logger = logger;
    }

    public async Task<AgentPlan> CreatePlanAsync(
        string goal,
        string userId,
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        var defaultAgent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        var agentId = defaultAgent?.Id ?? Guid.Empty;

        var plan = new AgentPlan
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            UserId = userId,
            SessionId = sessionId,
            Goal = goal,
            Status = "drafting",
            CurrentStepIndex = 0,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = userId
        };

        try
        {
            var skills = agentId != Guid.Empty
                ? await _skillRepository.GetEnabledAsync(agentId, cancellationToken)
                : new List<AgentSkill>();

            var (model, provider) = await GetCheapestModelAndProviderAsync();
            if (model == null || provider == null || skills.Count == 0)
            {
                _logger.LogWarning("Planning skipped — no LLM model/provider or no skills (model={Model}, skills={Count})",
                    model?.ApiModelId, skills.Count);
                plan.Status = "drafting";
                plan.StepsJson = "[]";
                return plan;
            }

            var skillCatalog = RenderSkillCatalog(skills);
            var request = new LLMProviderRequest
            {
                Message = $"Goal: {goal}\n\nAVAILABLE SKILLS ({skills.Count} total):\n{skillCatalog}",
                SystemPrompt = SystemPrompt,
                ModelId = model.ApiModelId,
                ConversationHistory = [],
                AvailableFunctions = [],
                Temperature = Temperature,
                MaxTokens = MaxOutputTokens,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken
            };

            var response = await provider.ProcessAsync(request);
            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("Planning LLM call returned no content for goal '{Goal}'", goal.ForLog());
                plan.StepsJson = "[]";
                return plan;
            }

            var steps = ParseSteps(response.Content);
            plan.StepsJson = JsonSerializer.Serialize(steps);
            _logger.LogInformation("Planned {Count} step(s) for goal '{Goal}' using model {Model}",
                steps.Count, goal.ForLog(), model.ApiModelId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PlanningAgent failed for goal '{Goal}' — returning empty plan", goal.ForLog());
            plan.Status = "failed";
            plan.LastErrorMessage = ex.Message;
            plan.StepsJson = "[]";
        }

        return plan;
    }

    private static string RenderSkillCatalog(IReadOnlyList<AgentSkill> skills)
    {
        var sb = new StringBuilder();
        foreach (var s in skills.Where(s => s.IsEnabled).Take(MaxSkillsInPrompt))
        {
            var desc = (s.Description ?? string.Empty).Replace('\n', ' ').Trim();
            if (desc.Length > 180) desc = desc[..180] + "...";
            sb.Append("- ").Append(s.Name).Append(": ").AppendLine(desc);
        }
        return sb.ToString();
    }

    private List<PlanStep> ParseSteps(string content)
    {
        var result = new List<PlanStep>();
        try
        {
            var json = ExtractJsonObject(content);
            if (string.IsNullOrWhiteSpace(json)) return result;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("steps", out var stepsElement) ||
                stepsElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            var order = 0;
            foreach (var stepEl in stepsElement.EnumerateArray())
            {
                if (++order > MaxSteps) break;
                if (!stepEl.TryGetProperty("skill", out var skillEl)) continue;
                var skillName = skillEl.GetString();
                if (string.IsNullOrWhiteSpace(skillName)) continue;

                var paramsMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (stepEl.TryGetProperty("params", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in paramsEl.EnumerateObject())
                    {
                        paramsMap[p.Name] = p.Value.ValueKind switch
                        {
                            JsonValueKind.String => p.Value.GetString(),
                            JsonValueKind.Number => p.Value.TryGetInt64(out var l) ? (object)l : p.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => p.Value.GetRawText()
                        };
                    }
                }

                var verify = stepEl.TryGetProperty("verifySkill", out var v) && v.ValueKind == JsonValueKind.String
                    ? v.GetString() : null;
                var reversible = stepEl.TryGetProperty("reversible", out var r)
                    ? (r.ValueKind == JsonValueKind.True || (r.ValueKind == JsonValueKind.False ? false : ReversibleSkills.Contains(skillName)))
                    : ReversibleSkills.Contains(skillName);

                result.Add(new PlanStep(order, skillName, paramsMap, verify, reversible));
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Planning JSON parse failed; returning empty step list");
        }
        return result;
    }

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        if (start < 0) return string.Empty;
        var depth = 0;
        for (var i = start; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0) return content[start..(i + 1)];
            }
        }
        return string.Empty;
    }

    private async Task<(LLMModel? model, ILLMProvider? provider)> GetCheapestModelAndProviderAsync()
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);
        var cheapest = models.OrderBy(m => m.CostPerInputToken + m.CostPerOutputToken).FirstOrDefault();
        if (cheapest == null) return (null, null);
        var provider = await _providerFactory.GetProviderForModelAsync(cheapest.ModelId);
        return (cheapest, provider);
    }
}
