// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the gate-passed description proposals, newest first, with the version columns and the live
/// description of their skill. The status and the field are part of the store query, so the limit is never
/// spent on rows that are filtered away afterwards.
/// </summary>
/// <param name="proposalRepository">Proposals by status and field</param>
/// <param name="agentSkillRepository">The skill row behind each proposal</param>

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class GetLearnedDescriptionExportCandidatesQueryHandler
    : IRequestHandler<GetLearnedDescriptionExportCandidatesQuery, IReadOnlyList<LearnedDescriptionExportCandidateDto>>
{
    private static readonly IReadOnlyList<string> ExportableStatuses = [ProposedChangeStatuses.GatePassed];

    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;

    public GetLearnedDescriptionExportCandidatesQueryHandler(
        IProposedSkillChangeRepository proposalRepository,
        IAgentSkillRepository agentSkillRepository)
    {
        _proposalRepository = proposalRepository;
        _agentSkillRepository = agentSkillRepository;
    }

    public async Task<IReadOnlyList<LearnedDescriptionExportCandidateDto>> Handle(
        GetLearnedDescriptionExportCandidatesQuery request, CancellationToken cancellationToken)
    {
        var proposals = await _proposalRepository.GetByStatusesAsync(
            ExportableStatuses, ProposedChangeFields.Description, request.Limit, cancellationToken);

        var candidates = new List<LearnedDescriptionExportCandidateDto>(proposals.Count);
        foreach (var proposal in proposals)
        {
            var skill = await _agentSkillRepository.GetByIdAsync(proposal.SkillId, cancellationToken);
            candidates.Add(new LearnedDescriptionExportCandidateDto(
                proposal.Id,
                proposal.SkillId,
                proposal.SkillName,
                proposal.ValueBefore,
                proposal.ValueAfter,
                skill?.Version,
                skill?.SeedVersion,
                skill?.Description,
                proposal.GateMetricsJson,
                proposal.ReviewedAt));
        }

        return candidates;
    }
}
