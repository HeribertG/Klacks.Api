// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Oracle O1. Answers the only question the learning loop can answer without a human: would this
/// utterance reach that skill right now. It runs the production toolset assembler - the same code path a
/// real chat turn takes - and reads off whether the target is among the tools it produced. No language
/// model is involved, so the verdict is reproducible and free apart from the local embedding and
/// reranking passes.
/// Probes run with administrator rights and an empty user identity on purpose: a permission the
/// triggering user happened to lack would otherwise look exactly like a routing gap, and per-user
/// guarantees (pending drafts, recipe state) would make the same utterance answer differently depending
/// on who asked last.
/// </summary>
/// <param name="agentRepository">Supplies the default agent the toolset is assembled for</param>
/// <param name="toolsetAssembler">The production toolset assembly the probe measures</param>
/// <param name="logger">Reports probes that could not be run at all</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillRoutingOracle : ISkillRoutingOracle
{
    private static readonly List<string> ProbeRights = [Roles.Admin];

    private readonly IAgentRepository _agentRepository;
    private readonly ISkillToolsetAssembler _toolsetAssembler;
    private readonly ILogger<SkillRoutingOracle> _logger;

    public SkillRoutingOracle(
        IAgentRepository agentRepository,
        ISkillToolsetAssembler toolsetAssembler,
        ILogger<SkillRoutingOracle> logger)
    {
        _agentRepository = agentRepository;
        _toolsetAssembler = toolsetAssembler;
        _logger = logger;
    }

    public async Task<SkillRoutingProbe> ProbeAsync(
        string utterance, string? locale, string targetSkill, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(utterance))
        {
            return new SkillRoutingProbe(false, []);
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            _logger.LogWarning("Routing probe skipped: no default agent");
            return new SkillRoutingProbe(false, []);
        }

        var toolset = await _toolsetAssembler.AssembleAsync(
            agent,
            [.. ProbeRights],
            utterance,
            conversationId: null,
            currentRoute: null,
            Guid.Empty.ToString(),
            locale,
            SkillLearningDefaults.RoutingProbeTopK,
            cancellationToken);

        var names = toolset.Functions.Select(function => function.Name).ToList();
        var found = !string.IsNullOrWhiteSpace(targetSkill)
            && names.Contains(targetSkill, StringComparer.OrdinalIgnoreCase);

        return new SkillRoutingProbe(found, names);
    }

    public async Task<IReadOnlyList<string>> FindFailingGoldenCasesAsync(
        IReadOnlyList<SkillLearningGoldenCase> goldenCases, CancellationToken cancellationToken = default)
    {
        var failing = new List<string>();

        foreach (var goldenCase in goldenCases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var probe = await ProbeAsync(
                goldenCase.Query, goldenCase.Locale, goldenCase.ExpectedSourceId, cancellationToken);

            if (!probe.TargetFound)
            {
                failing.Add(DescribeFailure(goldenCase));
            }
        }

        return failing;
    }

    private static string DescribeFailure(SkillLearningGoldenCase goldenCase) =>
        $"'{goldenCase.Query}' no longer reaches '{goldenCase.ExpectedSourceId}'";
}
