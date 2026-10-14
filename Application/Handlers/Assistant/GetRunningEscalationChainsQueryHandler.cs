// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the Running escalation chains the caller may see for the intervention list - only chains that still
/// need a human decision, matching the list's purpose (docs/HANDOFF-frontend-messenger-2026-08-16.md
/// §2.4). Both purposes (AbsenceCoverage and ProactiveApproval) are listed. An administrator sees every chain; everybody
/// else the chains of their visible groups plus every chain in which they hold an open stage. Reads the repository directly: this is a pure projection with no state transition, so it
/// does not need IEscalationChainService.
/// </summary>
/// <param name="chainRepository">Source of the Running chains and their stages.</param>
/// <param name="visibilityFilter">Narrows the chains to those the caller may see.</param>
/// <param name="userService">Tells whether the caller holds the Admin or Authorised role (drives CanCancel).</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetRunningEscalationChainsQueryHandler : IRequestHandler<GetRunningEscalationChainsQuery, IReadOnlyList<EscalationChainSummaryResource>>
{
    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationChainVisibilityFilter _visibilityFilter;
    private readonly IUserService _userService;

    public GetRunningEscalationChainsQueryHandler(
        IEscalationChainRepository chainRepository,
        IEscalationChainVisibilityFilter visibilityFilter,
        IUserService userService)
    {
        _chainRepository = chainRepository;
        _visibilityFilter = visibilityFilter;
        _userService = userService;
    }

    public async Task<IReadOnlyList<EscalationChainSummaryResource>> Handle(GetRunningEscalationChainsQuery request, CancellationToken cancellationToken)
    {
        var allChains = await _chainRepository.GetRunningChainsWithStagesAsync(cancellationToken);
        var chains = await _visibilityFilter.FilterVisibleAsync(allChains, request.CurrentUserId, cancellationToken);
        var isAdmin = await _userService.IsAdmin();
        var isSupervisor = _userService.GetRights().Contains(Roles.Authorised);

        return chains.Select(chain => new EscalationChainSummaryResource
        {
            Id = chain.Id,
            Purpose = chain.Purpose.ToString(),
            WorkId = chain.WorkId,
            ConditionId = chain.ConditionId,
            AbsentClientName = chain.AbsentClientName,
            ShiftStartUtc = chain.ShiftStartUtc,
            DeadlineUtc = chain.DeadlineUtc,
            CanAcknowledge = chain.Stages.Any(s =>
                s.UserId == request.CurrentUserId && s.Status == EscalationStageStatus.Notified),
            CanCancel = EscalationChainCancelPolicy.MayCancel(chain, request.CurrentUserId, isAdmin, isSupervisor),
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
