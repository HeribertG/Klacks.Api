// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists the per-user autonomy level via upsert on agent_autonomy_preferences.
/// </summary>
/// <param name="repository">Per-user autonomy preference storage.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SetAutonomyLevelCommandHandler : IRequestHandler<SetAutonomyLevelCommand, AutonomyLevel>
{
    private readonly IAgentAutonomyPreferenceRepository _repository;

    public SetAutonomyLevelCommandHandler(IAgentAutonomyPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<AutonomyLevel> Handle(SetAutonomyLevelCommand request, CancellationToken cancellationToken)
    {
        var row = new AgentAutonomyPreferenceRow
        {
            UserId = request.UserId.ToString(),
            Level = request.Level
        };

        var saved = await _repository.UpsertAsync(row, cancellationToken);
        return saved.Level;
    }
}
