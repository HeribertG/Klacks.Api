// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving all AnalyseScenarios of a group. A group outside the caller's visibility - or the
/// group-less scenarios for a restricted caller - yields an empty list, exactly like a group without scenarios.
/// </summary>
/// <param name="GroupId">ID of the group whose scenarios are loaded; null loads the group-less scenarios</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the requested group</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class ListAnalyseScenariosQueryHandler : BaseHandler, IRequestHandler<ListAnalyseScenariosQuery, List<AnalyseScenarioResource>>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public ListAnalyseScenariosQueryHandler(
        IAnalyseScenarioRepository repository,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<ListAnalyseScenariosQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<List<AnalyseScenarioResource>> Handle(ListAnalyseScenariosQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsScenarioScopeVisibleAsync(request.GroupId, cancellationToken))
            {
                return new List<AnalyseScenarioResource>();
            }

            var scenarios = await _repository.GetByGroupAsync(request.GroupId, cancellationToken);

            return scenarios.Select(s => new AnalyseScenarioResource
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                GroupId = s.GroupId,
                FromDate = s.FromDate,
                UntilDate = s.UntilDate,
                Token = s.Token,
                RunGroupId = s.RunGroupId,
                CreatedByUser = s.CreatedByUser,
                Status = (int)s.Status,
                SubScoreJson = s.SubScoreJson,
                ChurnRatio = s.ChurnRatio,
                Stage0Violations = s.Stage0Violations,
            }).ToList();
        }, nameof(Handle), new { request.GroupId });
    }
}
