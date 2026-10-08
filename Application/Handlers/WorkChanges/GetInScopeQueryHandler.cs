// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles GetWorkChangeInScopeQuery: loads the WorkChange in any scope, then answers it only when its parent Work's
/// AnalyseToken (the scope is the Work's, so legacy rows with a drifted own token still resolve correctly) equals the
/// requested scope and every client it touches (WorkChangeTouchedClients: the parent Work's owner and the
/// replacement client) is visible to the caller. Every other case - missing, other scope, hidden client - throws
/// the same KeyNotFoundException, so the scope is no existence oracle.
/// </summary>
/// <param name="workChangeRepository">Loads the WorkChange with its parent Work regardless of scope</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the touched clients</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class GetInScopeQueryHandler : BaseHandler, IRequestHandler<GetWorkChangeInScopeQuery, WorkChangeResource>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetInScopeQueryHandler(
        IWorkChangeRepository workChangeRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetInScopeQueryHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkChangeResource> Handle(GetWorkChangeInScopeQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workChange = await _workChangeRepository.GetWithWorkInAnyScope(request.Id);

            if (workChange?.Work == null
                || workChange.Work.AnalyseToken != request.AnalyseToken
                || !await _clientVisibilityGuard.AreAllVisibleAsync(
                    WorkChangeTouchedClients.Of(workChange.Work, workChange.ReplaceClientId), cancellationToken))
            {
                throw new KeyNotFoundException($"WorkChange with ID {request.Id} not found");
            }

            return _scheduleMapper.ToWorkChangeResource(workChange);
        }, nameof(Handle), new { request.Id });
    }
}
