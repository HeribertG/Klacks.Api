// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Starts a learning run on demand. The work itself is handed to the launcher, which owns the guarantee
/// that the scheduled tick and this trigger never run at the same time and returns immediately either way.
/// </summary>
/// <param name="launcher">Owns the single-run gate and the background scope</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class RunSkillLearningCommandHandler : IRequestHandler<RunSkillLearningCommand, SkillLearningRunResponse>
{
    private readonly ISkillLearningRunLauncher _launcher;

    public RunSkillLearningCommandHandler(ISkillLearningRunLauncher launcher)
    {
        _launcher = launcher;
    }

    public Task<SkillLearningRunResponse> Handle(
        RunSkillLearningCommand request, CancellationToken cancellationToken)
    {
        var ticket = _launcher.StartDetached();
        return Task.FromResult(new SkillLearningRunResponse(ticket.Started, ticket.Reason));
    }
}
