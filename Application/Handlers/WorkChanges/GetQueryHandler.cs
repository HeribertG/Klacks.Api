// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a single WorkChange by id in whatever scope it is stored: the scope comes from the stored row and its parent
/// Work, because the edit dialogs of a scenario address the row by id only. A change whose parent Work owner, replacement
/// client or scenario is hidden from the caller is answered exactly like a change that does not exist.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the touched clients and the scenario of the change</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<WorkChangeResource>, WorkChangeResource>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IWorkChangeRepository workChangeRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkChangeResource> Handle(GetQuery<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workChange = await _workChangeRepository.GetWithWorkInAnyScope(request.Id);

            if (workChange == null
                || !await _rowAccessGuard.AreAllVisibleAsync(
                    WorkChangeTouchedClients.Of(workChange.Work, workChange.ReplaceClientId),
                    workChange.Work?.AnalyseToken ?? workChange.AnalyseToken,
                    cancellationToken))
            {
                throw new KeyNotFoundException($"WorkChange with ID {request.Id} not found");
            }

            return _scheduleMapper.ToWorkChangeResource(workChange);
        }, nameof(Handle), new { request.Id });
    }
}
