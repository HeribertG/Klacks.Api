// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the launcher's in-memory status of learning runs for the run-status endpoint.
/// </summary>
/// <param name="launcher">Owns the single-run gate and the status of the latest run</param>

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class GetSkillLearningRunStatusQueryHandler
    : IRequestHandler<GetSkillLearningRunStatusQuery, SkillLearningRunStatusResponse>
{
    private readonly ISkillLearningRunLauncher _launcher;

    public GetSkillLearningRunStatusQueryHandler(ISkillLearningRunLauncher launcher)
    {
        _launcher = launcher;
    }

    public Task<SkillLearningRunStatusResponse> Handle(
        GetSkillLearningRunStatusQuery request, CancellationToken cancellationToken)
    {
        var status = _launcher.GetStatus();
        return Task.FromResult(new SkillLearningRunStatusResponse(
            status.Running, status.LastStartedUtc, status.LastFinishedUtc,
            status.LastSucceeded, status.LastError, status.LastSummary, status.LastTrigger));
    }
}
