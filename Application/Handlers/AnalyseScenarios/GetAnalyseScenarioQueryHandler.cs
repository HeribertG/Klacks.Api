// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving a single AnalyseScenario by ID.
/// </summary>
/// <param name="Id">ID of the requested scenario</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class GetAnalyseScenarioQueryHandler : BaseHandler, IRequestHandler<GetAnalyseScenarioQuery, AnalyseScenarioResource?>
{
    private readonly IAnalyseScenarioRepository _repository;

    public GetAnalyseScenarioQueryHandler(
        IAnalyseScenarioRepository repository,
        ILogger<GetAnalyseScenarioQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<AnalyseScenarioResource?> Handle(GetAnalyseScenarioQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(request.Id);

            if (scenario == null)
            {
                return null;
            }

            return new AnalyseScenarioResource
            {
                Id = scenario.Id,
                Name = scenario.Name,
                Description = scenario.Description,
                GroupId = scenario.GroupId,
                FromDate = scenario.FromDate,
                UntilDate = scenario.UntilDate,
                Token = scenario.Token,
                CreatedByUser = scenario.CreatedByUser,
                Status = (int)scenario.Status
            };
        }, nameof(Handle), new { request.Id });
    }
}
