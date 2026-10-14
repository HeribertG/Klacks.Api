// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

/// <summary>
/// Returns the autofill runs in progress whose group the caller may see. Fail-closed: the scope is resolved for the
/// caller's explicit user id, so a request without one sees nothing; a run without a group is shown to unrestricted
/// callers only. A hidden run is left out, never answered with an error.
/// </summary>
/// <param name="presenceTracker">The runs in progress, unfiltered</param>
/// <param name="groupVisibilityService">Resolves the caller's visible groups</param>
/// <param name="userService">Identifies the caller</param>
public sealed class GetActiveAutofillRunsQueryHandler
    : IRequestHandler<GetActiveAutofillRunsQuery, IReadOnlyList<AutofillRunPresenceDto>>
{
    private readonly IAutofillRunPresenceTracker _presenceTracker;
    private readonly IGroupVisibilityService _groupVisibilityService;
    private readonly IUserService _userService;

    public GetActiveAutofillRunsQueryHandler(
        IAutofillRunPresenceTracker presenceTracker,
        IGroupVisibilityService groupVisibilityService,
        IUserService userService)
    {
        _presenceTracker = presenceTracker;
        _groupVisibilityService = groupVisibilityService;
        _userService = userService;
    }

    public async Task<IReadOnlyList<AutofillRunPresenceDto>> Handle(
        GetActiveAutofillRunsQuery request, CancellationToken cancellationToken)
    {
        var runs = _presenceTracker.GetActive();
        if (runs.Count == 0)
        {
            return [];
        }

        var scope = await _groupVisibilityService.GetVisibilityScopeForUserAsync(
            _userService.GetIdString(), cancellationToken);
        var visibleGroupIds = scope.VisibleGroupIds.ToHashSet();

        return runs.Where(run => IsVisible(run, scope, visibleGroupIds)).ToList();
    }

    private static bool IsVisible(AutofillRunPresenceDto run, GroupVisibilityScope scope, HashSet<Guid> visibleGroupIds)
        => scope.IsUnrestricted || (run.GroupId is { } groupId && visibleGroupIds.Contains(groupId));
}
