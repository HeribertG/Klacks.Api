// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the escalation chains of the last week that ended without anybody taking care of them - cancelled by a
/// planner or lapsed unanswered - for the "not resolved" card of the intervention list (Owner decision 2026-10-15).
/// Visibility follows the running list's group rule, plus everybody who held a stage of the chain or cancelled it.
/// The free-text cancel reason is shown only to an administrator, a supervisor and whoever cancelled, so a reason can
/// stay honest without being read by the whole group.
/// </summary>
/// <param name="chainRepository">Source of the ended chains and their display context.</param>
/// <param name="visibilityFilter">Narrows the chains to those the caller may see.</param>
/// <param name="userService">Tells whether the caller holds the Admin or Authorised role (drives the reason visibility).</param>
/// <param name="timeProvider">Anchors the one-week window.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Escalation;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetUnresolvedEscalationChainsQueryHandler
    : IRequestHandler<GetUnresolvedEscalationChainsQuery, IReadOnlyList<UnresolvedEscalationChainResource>>
{
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromDays(7);

    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationChainVisibilityFilter _visibilityFilter;
    private readonly IUserService _userService;
    private readonly TimeProvider _timeProvider;

    public GetUnresolvedEscalationChainsQueryHandler(
        IEscalationChainRepository chainRepository,
        IEscalationChainVisibilityFilter visibilityFilter,
        IUserService userService,
        TimeProvider timeProvider)
    {
        _chainRepository = chainRepository;
        _visibilityFilter = visibilityFilter;
        _userService = userService;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<UnresolvedEscalationChainResource>> Handle(
        GetUnresolvedEscalationChainsQuery request, CancellationToken cancellationToken)
    {
        var sinceUtc = _timeProvider.GetUtcNow().UtcDateTime - HistoryWindow;
        var ended = await _chainRepository.GetUnresolvedChainsSinceAsync(sinceUtc, cancellationToken);
        var chains = await _visibilityFilter.FilterVisibleEndedAsync(ended, request.CurrentUserId, cancellationToken);

        var isAdmin = await _userService.IsAdmin();
        var isSupervisor = _userService.GetRights().Contains(Roles.Authorised);

        var display = await _chainRepository.GetDisplayContextAsync(
            chains.Where(c => c.GroupId.HasValue).Select(c => c.GroupId!.Value).Distinct().ToList(),
            chains.Where(c => c.ConditionId.HasValue).Select(c => c.ConditionId!.Value).Distinct().ToList(),
            chains.Where(c => c.ScenarioId.HasValue).Select(c => c.ScenarioId!.Value).Distinct().ToList(),
            cancellationToken);

        return chains.Select(chain => ToResource(chain, display, request.CurrentUserId, isAdmin || isSupervisor)).ToList();
    }

    private static UnresolvedEscalationChainResource ToResource(
        EscalationChain chain, EscalationChainDisplayContext display, string currentUserId, bool mayReadAnyReason)
    {
        var mayReadReason = mayReadAnyReason
            || string.Equals(chain.CancelledByUserId, currentUserId, StringComparison.OrdinalIgnoreCase);
        var hasReason = !string.IsNullOrWhiteSpace(chain.CancelReason);
        AnalyseScenarioStatus? scenarioStatus = chain.ScenarioId is Guid scenarioId
            && display.ScenarioStatuses.TryGetValue(scenarioId, out var status)
                ? status
                : null;

        return new UnresolvedEscalationChainResource
        {
            Id = chain.Id,
            Purpose = chain.Purpose.ToString(),
            Status = chain.Status.ToString(),
            AbsentClientName = chain.AbsentClientName,
            ShiftStartUtc = chain.ShiftStartUtc,
            GroupId = chain.GroupId,
            GroupName = chain.GroupId is Guid groupId ? display.GroupNames.GetValueOrDefault(groupId) : null,
            FindingKind = chain.ConditionId is Guid conditionId ? display.FindingKinds.GetValueOrDefault(conditionId) : null,
            EndedAtUtc = chain.CancelledAtUtc ?? chain.ExhaustedAtUtc,
            CancelledByUserName = chain.CancelledByUserName,
            CancelReason = hasReason && mayReadReason ? chain.CancelReason : null,
            CancelReasonHidden = hasReason && !mayReadReason,
            NotifiedStageCount = chain.Stages.Count(s => s.NotifiedAtUtc.HasValue),
            ScenarioStatus = scenarioStatus?.ToString(),
            ScenarioId = scenarioStatus == AnalyseScenarioStatus.Active ? chain.ScenarioId : null
        };
    }
}
