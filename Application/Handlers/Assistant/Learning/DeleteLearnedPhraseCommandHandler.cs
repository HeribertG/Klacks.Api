// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Withdraws one row of the "phrasings" section. Nothing is erased: a learned phrase goes to Rejected and
/// a proposal goes to rejected, both of which keep the row as a record that this wording was tried and
/// discarded. That record is what stops a later learning round from proposing the very same phrase again.
/// A description the loop already applied by itself is a special case: rejecting it has to put the old
/// description back, or the card would say "discarded" while the change stays live. The restore is skipped
/// when the description no longer matches what the proposal wrote - something else changed it since, and
/// overwriting that would be the silent data loss the stale check in the approval path exists to prevent.
/// That skip is reported as a conflict instead of as plain success, because the outcome differs from what
/// the administrator asked for: the row is discarded, the live description is not the one they saw.
/// A proposal already exported into skill-seeds.json is left untouched only while the live description still
/// equals what was exported - the export reached a release and the seed loader reseeded it, so rejecting here
/// would disagree with what is actually still live. mark-exported runs before a human reviews the export
/// branch, so an exported proposal whose live description differs (still the old wording, or something else
/// entirely) never got that far: the export branch was discarded and this rejection is the reviewer's veto, not
/// a disagreement with a release.
/// </summary>
/// <param name="phraseRepository">Learned trigger phrases</param>
/// <param name="proposalRepository">Proposed description changes</param>
/// <param name="agentSkillRepository">Carries the description an automatically applied change has to be undone on</param>
/// <param name="catalogRefresher">Rebuilds the skill catalogue so a withdrawn phrase stops influencing retrieval</param>
/// <param name="logger">Records a description that could not be put back</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class DeleteLearnedPhraseCommandHandler
    : IRequestHandler<DeleteLearnedPhraseCommand, LearningMutationResult>
{
    private const string RefreshReason = "learned phrase withdrawn by an administrator";

    private const string RevertReason = "automatically applied description change withdrawn by an administrator";

    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillCatalogRefresher _catalogRefresher;
    private readonly ILogger<DeleteLearnedPhraseCommandHandler> _logger;

    public DeleteLearnedPhraseCommandHandler(
        ISkillPhraseRepository phraseRepository,
        IProposedSkillChangeRepository proposalRepository,
        IAgentSkillRepository agentSkillRepository,
        ISkillCatalogRefresher catalogRefresher,
        ILogger<DeleteLearnedPhraseCommandHandler> logger)
    {
        _phraseRepository = phraseRepository;
        _proposalRepository = proposalRepository;
        _agentSkillRepository = agentSkillRepository;
        _catalogRefresher = catalogRefresher;
        _logger = logger;
    }

    public async Task<LearningMutationResult> Handle(
        DeleteLearnedPhraseCommand request, CancellationToken cancellationToken)
    {
        if (await _phraseRepository.SetStatusAsync(request.Id, SkillPhraseStatuses.Rejected, cancellationToken))
        {
            await _catalogRefresher.RefreshAsync(RefreshReason, cancellationToken);
            return LearningMutationResult.Success();
        }

        var proposal = await _proposalRepository.GetByIdAsync(request.Id, cancellationToken);
        if (proposal == null)
        {
            return LearningMutationResult.NotFound();
        }

        if (proposal.Status == ProposedChangeStatuses.Exported && await IsReseededAsync(proposal, cancellationToken))
        {
            return LearningMutationResult.AlreadyExported();
        }

        var wasApplied = proposal.Status == ProposedChangeStatuses.AppliedAuto;
        var reverted = await TryRevertAppliedChangeAsync(proposal, cancellationToken);

        proposal.Status = ProposedChangeStatuses.Rejected;
        proposal.ReviewedAt = DateTime.UtcNow;
        proposal.UpdateTime = DateTime.UtcNow;
        await _proposalRepository.UpdateAsync(proposal, cancellationToken);

        if (reverted)
        {
            await _catalogRefresher.RefreshAsync(RevertReason, cancellationToken);
            return LearningMutationResult.Success();
        }

        if (wasApplied)
        {
            _logger.LogWarning(
                "Proposal {ProposalId} for skill {Name} was rejected, but its description could not be put "
                    + "back: the live description no longer matches what the proposal applied",
                proposal.Id, proposal.SkillName);

            return LearningMutationResult.StaleDescription();
        }

        return LearningMutationResult.Success();
    }

    // The seed loader reseeds a skill from an exported proposal only after the release that shipped it. Until
    // then, or when the export branch was discarded, the live description never reached this proposal's
    // ValueAfter and rejecting the proposal here does not disagree with anything actually live.
    private async Task<bool> IsReseededAsync(ProposedSkillChange proposal, CancellationToken cancellationToken)
    {
        var skill = await _agentSkillRepository.GetByIdAsync(proposal.SkillId, cancellationToken);
        return skill != null && string.Equals(skill.Description, proposal.ValueAfter, StringComparison.Ordinal);
    }

    private async Task<bool> TryRevertAppliedChangeAsync(
        ProposedSkillChange proposal, CancellationToken cancellationToken)
    {
        if (proposal.Status != ProposedChangeStatuses.AppliedAuto)
        {
            return false;
        }

        var skill = await _agentSkillRepository.GetByIdAsync(proposal.SkillId, cancellationToken);
        if (skill == null || !string.Equals(skill.Description, proposal.ValueAfter, StringComparison.Ordinal))
        {
            return false;
        }

        skill.Description = proposal.ValueBefore;
        skill.Version += 1;
        await _agentSkillRepository.UpdateAsync(skill, cancellationToken);

        return true;
    }
}
