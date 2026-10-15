// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Implements the chain visibility rule: a holder of a Pending or Notified stage of the chain always sees it; otherwise
/// the decision goes through IPlanningAudienceResolver.MaySeeAnyGroupAsync. That check is ROOT-based, like the
/// escalation roster: a chain's group is visible when its Nested Set root is one of the caller's own GroupVisibility
/// roots (admins always; a chain without a group, a user without visibility rows and an unknown group are hidden). This
/// differs from the shift check of get_replacement_status, which uses IGroupVisibilityGuard and therefore the caller's
/// expanded visible group ids (subgroups included) - the two answers can differ for a group below a visible root and are
/// deliberately not unified here. The group verdict is computed once per distinct group of a list.
/// </summary>
/// <param name="audienceResolver">Resolves whether a user may see a group, admins included</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public class EscalationChainVisibilityFilter : IEscalationChainVisibilityFilter
{
    private readonly IPlanningAudienceResolver _audienceResolver;

    public EscalationChainVisibilityFilter(IPlanningAudienceResolver audienceResolver)
    {
        _audienceResolver = audienceResolver;
    }

    public Task<List<EscalationChain>> FilterVisibleAsync(
        IReadOnlyCollection<EscalationChain> chains, string userId, CancellationToken cancellationToken = default)
    {
        return FilterAsync(chains, userId, chain => HoldsOpenStage(chain, userId), cancellationToken);
    }

    public Task<List<EscalationChain>> FilterVisibleEndedAsync(
        IReadOnlyCollection<EscalationChain> chains, string userId, CancellationToken cancellationToken = default)
    {
        return FilterAsync(chains, userId, chain => WasInvolved(chain, userId), cancellationToken);
    }

    private async Task<List<EscalationChain>> FilterAsync(
        IReadOnlyCollection<EscalationChain> chains,
        string userId,
        Func<EscalationChain, bool> alwaysVisible,
        CancellationToken cancellationToken)
    {
        var verdictByGroup = new Dictionary<Guid, bool>();
        var visible = new List<EscalationChain>();

        foreach (var chain in chains)
        {
            if (alwaysVisible(chain))
            {
                visible.Add(chain);
                continue;
            }

            var groupKey = chain.GroupId ?? Guid.Empty;
            if (!verdictByGroup.TryGetValue(groupKey, out var mayRead))
            {
                mayRead = await MaySeeAsync(chain.GroupId, userId, cancellationToken);
                verdictByGroup[groupKey] = mayRead;
            }

            if (mayRead)
            {
                visible.Add(chain);
            }
        }

        return visible;
    }

    public async Task<bool> IsVisibleAsync(EscalationChain chain, string userId, CancellationToken cancellationToken = default)
    {
        return HoldsOpenStage(chain, userId) || await MaySeeAsync(chain.GroupId, userId, cancellationToken);
    }

    private static bool HoldsOpenStage(EscalationChain chain, string userId)
    {
        return chain.Stages.Any(s =>
            string.Equals(s.UserId, userId, StringComparison.OrdinalIgnoreCase)
            && (s.Status == EscalationStageStatus.Pending || s.Status == EscalationStageStatus.Notified));
    }

    private static bool WasInvolved(EscalationChain chain, string userId)
    {
        return string.Equals(chain.CancelledByUserId, userId, StringComparison.OrdinalIgnoreCase)
            || chain.Stages.Any(s => string.Equals(s.UserId, userId, StringComparison.OrdinalIgnoreCase));
    }

    private Task<bool> MaySeeAsync(Guid? groupId, string userId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid> groupIds = groupId.HasValue ? [groupId.Value] : [];
        return _audienceResolver.MaySeeAnyGroupAsync(userId, groupIds, cancellationToken);
    }
}
