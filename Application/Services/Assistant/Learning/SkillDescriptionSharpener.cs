// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the description proposals the optimizer left pending, but only those that leave every golden
/// case routing exactly as before. A description is the largest part of a skill's index text, so
/// tightening one moves that skill's vector and can push a neighbouring skill out of reach for a query
/// nobody proposed anything about. There is no way to predict that: the change has to be applied, the
/// index rebuilt and the goldset replayed, and rolled back when it turned something red.
/// A case that was already failing before the change is not a regression - the baseline is measured once,
/// before the first proposal is touched, and carried forward as each accepted proposal shifts it.
/// The gate can only be measured on a description that is actually live, because the assembler reads the
/// skill catalogue and the knowledge index rather than a candidate value. The description is therefore
/// set, measured and put back again - and it is put back on a failing gate exactly as on a red one, so a
/// probe that throws half way through cannot leave a never-judged description in the catalogue.
/// The optimizer is asked for new proposals at the start of the same pass: with the manual generate
/// endpoint gone, this is the only writer left, and a gate with nothing to gate would be dead code. It
/// returns without calling a model when nobody corrected a skill choice since the last run.
/// </summary>
/// <param name="optimizer">Turns recent wrong-skill corrections into new pending proposals</param>
/// <param name="proposalRepository">Pending proposals and their verdicts</param>
/// <param name="agentSkillRepository">The skill row the description lives on</param>
/// <param name="goldenCaseRepository">The goldset the gate replays</param>
/// <param name="routingOracle">Runs the replay</param>
/// <param name="catalogRefresher">Rebuilds cache, registry and knowledge index after each change</param>
/// <param name="logger">One line per decision</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillDescriptionSharpener : ISkillDescriptionSharpener
{
    private const int TrajectoriesToAnalyze = 30;

    private readonly ISkillDescriptionOptimizer _optimizer;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillLearningGoldenCaseRepository _goldenCaseRepository;
    private readonly ISkillRoutingOracle _routingOracle;
    private readonly ISkillCatalogRefresher _catalogRefresher;
    private readonly ILogger<SkillDescriptionSharpener> _logger;

    public SkillDescriptionSharpener(
        ISkillDescriptionOptimizer optimizer,
        IProposedSkillChangeRepository proposalRepository,
        IAgentSkillRepository agentSkillRepository,
        ISkillLearningGoldenCaseRepository goldenCaseRepository,
        ISkillRoutingOracle routingOracle,
        ISkillCatalogRefresher catalogRefresher,
        ILogger<SkillDescriptionSharpener> logger)
    {
        _optimizer = optimizer;
        _proposalRepository = proposalRepository;
        _agentSkillRepository = agentSkillRepository;
        _goldenCaseRepository = goldenCaseRepository;
        _routingOracle = routingOracle;
        _catalogRefresher = catalogRefresher;
        _logger = logger;
    }

    public async Task<(int Applied, int Blocked)> RunAsync(CancellationToken cancellationToken = default)
    {
        await _optimizer.GenerateProposalsAsync(TrajectoriesToAnalyze, cancellationToken);

        var pending = await _proposalRepository.GetPendingAsync(
            SkillLearningDefaults.MaxProposalsPerRun, cancellationToken);

        if (pending.Count == 0)
        {
            return (0, 0);
        }

        var goldenCases = await _goldenCaseRepository.ListAsync(
            SkillLearningDefaults.MaxGoldenCasesPerRegressionCheck, cancellationToken);

        var baseline = await _routingOracle.FindFailingGoldenCasesAsync(goldenCases, cancellationToken);

        var applied = 0;
        var blocked = 0;

        foreach (var proposal in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outcome = await DecideAsync(proposal, goldenCases, baseline, cancellationToken);
            if (outcome == null)
            {
                continue;
            }

            baseline = outcome.Baseline;

            if (outcome.Applied)
            {
                applied++;
            }
            else
            {
                blocked++;
            }
        }

        return (applied, blocked);
    }

    private async Task<Decision?> DecideAsync(
        ProposedSkillChange proposal,
        IReadOnlyList<SkillLearningGoldenCase> goldenCases,
        IReadOnlyList<string> baseline,
        CancellationToken cancellationToken)
    {
        if (proposal.Field != ProposedChangeFields.Description)
        {
            return null;
        }

        var skill = await _agentSkillRepository.GetByIdAsync(proposal.SkillId, cancellationToken);
        if (skill == null)
        {
            return null;
        }

        var original = skill.Description;
        if (!string.Equals(original, proposal.ValueBefore, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Skill {Name} changed since proposal {ProposalId} was generated; leaving it pending",
                skill.Name, proposal.Id);
            return null;
        }

        skill.Description = proposal.ValueAfter;
        skill.Version += 1;
        await _agentSkillRepository.UpdateAsync(skill, cancellationToken);
        await _catalogRefresher.RefreshAsync($"applying description proposal {proposal.Id}", cancellationToken);

        IReadOnlyList<string> failing;
        try
        {
            failing = await _routingOracle.FindFailingGoldenCasesAsync(goldenCases, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RestoreAsync(skill, original, proposal.Id, cancellationToken);
            _logger.LogWarning(
                exception,
                "The regression gate could not be measured for proposal {ProposalId}; the description of "
                    + "skill {Name} was put back and the proposal stays pending",
                proposal.Id, skill.Name);
            return null;
        }

        var regressions = failing.Except(baseline, StringComparer.Ordinal).ToList();

        if (regressions.Count == 0)
        {
            await MarkAsync(proposal, ProposedChangeStatuses.AppliedAuto, cancellationToken);
            _logger.LogInformation(
                "Description of skill {Name} sharpened automatically (proposal {ProposalId})", skill.Name, proposal.Id);
            return new Decision(true, failing);
        }

        await RestoreAsync(skill, original, proposal.Id, cancellationToken);

        proposal.Justification = Describe(regressions);
        await MarkAsync(proposal, ProposedChangeStatuses.BlockedRegression, cancellationToken);

        _logger.LogWarning(
            "Description proposal {ProposalId} for skill {Name} blocked: {Count} golden case(s) would break",
            proposal.Id, skill.Name, regressions.Count);

        return new Decision(false, baseline);
    }

    private async Task RestoreAsync(
        AgentSkill skill, string original, Guid proposalId, CancellationToken cancellationToken)
    {
        skill.Description = original;
        skill.Version += 1;
        await _agentSkillRepository.UpdateAsync(skill, cancellationToken);
        await _catalogRefresher.RefreshAsync($"reverting description proposal {proposalId}", cancellationToken);
    }

    private async Task MarkAsync(ProposedSkillChange proposal, string status, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        proposal.Status = status;
        proposal.ReviewedBy = SkillLearningDefaults.AutomaticReviewer;
        proposal.ReviewedAt = now;
        proposal.UpdateTime = now;
        await _proposalRepository.UpdateAsync(proposal, cancellationToken);
    }

    private static string Describe(IReadOnlyList<string> regressions) =>
        "Blocked by the routing regression gate: " + string.Join("; ", regressions);

    private sealed record Decision(bool Applied, IReadOnlyList<string> Baseline);
}
