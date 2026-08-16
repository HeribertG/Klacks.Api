// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the escalation call list for a group's admin reorder UI, re-deriving it from the current
/// GroupVisibility membership first so a stale row never shows.
/// </summary>
/// <param name="rosterService">Re-derives and resolves the roster rows for a group.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetEscalationRosterQueryHandler : IRequestHandler<GetEscalationRosterQuery, IReadOnlyList<EscalationRosterEntryResource>>
{
    private readonly IEscalationRosterService _rosterService;

    public GetEscalationRosterQueryHandler(IEscalationRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    public async Task<IReadOnlyList<EscalationRosterEntryResource>> Handle(GetEscalationRosterQuery request, CancellationToken cancellationToken)
    {
        var entries = await _rosterService.GetRosterEntriesAsync(request.GroupId, cancellationToken);

        return entries.Select(entry => new EscalationRosterEntryResource
        {
            Id = entry.Id,
            UserId = entry.UserId,
            DisplayName = entry.DisplayName,
            EffectiveRank = entry.EffectiveRank,
            HasOverride = entry.HasOverride,
            IsOrphaned = entry.IsOrphaned
        }).ToList();
    }
}
