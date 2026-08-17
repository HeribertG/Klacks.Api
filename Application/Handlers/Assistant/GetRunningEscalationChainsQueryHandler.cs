// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists every Running escalation chain for the admin intervention list - only chains that still
/// need a human decision, matching the list's purpose (docs/HANDOFF-frontend-messenger-2026-08-16.md
/// §2.4). Reads the repository directly: this is a pure projection with no state transition, so it
/// does not need IEscalationChainService.
/// </summary>
/// <param name="chainRepository">Source of the Running chains and their stages.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetRunningEscalationChainsQueryHandler : IRequestHandler<GetRunningEscalationChainsQuery, IReadOnlyList<EscalationChainSummaryResource>>
{
    private readonly IEscalationChainRepository _chainRepository;

    public GetRunningEscalationChainsQueryHandler(IEscalationChainRepository chainRepository)
    {
        _chainRepository = chainRepository;
    }

    public async Task<IReadOnlyList<EscalationChainSummaryResource>> Handle(GetRunningEscalationChainsQuery request, CancellationToken cancellationToken)
    {
        var chains = await _chainRepository.GetRunningChainsWithStagesAsync(cancellationToken);

        return chains.Select(chain => new EscalationChainSummaryResource
        {
            Id = chain.Id,
            WorkId = chain.WorkId,
            AbsentClientName = chain.AbsentClientName,
            ShiftStartUtc = chain.ShiftStartUtc,
            DeadlineUtc = chain.DeadlineUtc,
            CanAcknowledge = chain.Stages.Any(s =>
                s.UserId == request.CurrentUserId && s.Status == EscalationStageStatus.Notified),
            Stages = chain.Stages.OrderBy(s => s.Rank).Select(s => new EscalationStageSummaryResource
            {
                Rank = s.Rank,
                UserId = s.UserId,
                UserDisplayName = s.UserDisplayName,
                Status = s.Status.ToString(),
                NotifiedAtUtc = s.NotifiedAtUtc,
                DueAtUtc = s.DueAtUtc,
                RespondedAtUtc = s.RespondedAtUtc
            }).ToList()
        }).ToList();
    }
}
