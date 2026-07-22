// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Delegates an analysis-style question ("analyze the July roster and tell me where the gaps are") to a
/// bounded read-only research sub-loop that runs on the cheapest available model. The sub-loop gathers
/// data with read-only tools and returns ONE compact English synthesis; the outer turn receives only that
/// synthesis (in the skill message), never the intermediate tool outputs, which is the whole point — it
/// keeps the outer context small and off the premium model. Read-only, so it needs no confirmation.
/// </summary>
/// <param name="question">The natural-language analysis question to research and answer.</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(ReadOnlyResearchConstants.RunAnalysisSkillName)]
public class RunAnalysisSkill : BaseSkillImplementation
{
    private const string QuestionParameter = "question";

    private readonly IReadOnlyResearchService _researchService;

    public RunAnalysisSkill(IReadOnlyResearchService researchService)
    {
        _researchService = researchService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var question = GetRequiredString(parameters, QuestionParameter);

        var research = await _researchService.ResearchAsync(question, context, cancellationToken);

        var summary = new
        {
            research.IterationsUsed,
            research.ToolCallCount,
            research.ToolsUsed,
            research.ModelAvailable
        };

        return SkillResult.SuccessResult(summary, research.Synthesis);
    }
}
