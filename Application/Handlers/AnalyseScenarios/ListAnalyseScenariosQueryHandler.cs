// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving all AnalyseScenarios of a group.
/// </summary>
/// <param name="GroupId">ID of the group whose scenarios are loaded</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class ListAnalyseScenariosQueryHandler : BaseHandler, IRequestHandler<ListAnalyseScenariosQuery, List<AnalyseScenarioResource>>
{
    private readonly IAnalyseScenarioRepository _repository;

    public ListAnalyseScenariosQueryHandler(
        IAnalyseScenarioRepository repository,
        ILogger<ListAnalyseScenariosQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<List<AnalyseScenarioResource>> Handle(ListAnalyseScenariosQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
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
                CreatedByUser = s.CreatedByUser,
                Status = (int)s.Status
            }).ToList();
        }, nameof(Handle), new { request.GroupId });
    }
}
